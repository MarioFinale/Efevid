Option Strict On
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions
Imports Efevid.Ephe
Imports MWBot.net.WikiBot
Imports MWBot.net.Utility
Imports MWBot.net.Utility.Utils
Imports Svg
Imports Image = System.Drawing.Image

Public Class NewVideoGen

    Private IntroImageXmlContent As String
    Private EphFrameXmlContent As String
    Private OutroImageXmlContent As String
    Private Ephs As WikiEphe()
    Private CurrentDate As Date
    Private OutroText As String
    Private OutputDirectory As String
    Private EffectiveFrames As Integer = 0
    Private WorkerBot As Bot

    Sub New(ByRef bot As Bot, ByVal introXmlPaths As String(), outDir As String, currentEpheDate As Date, currentEphs As WikiEphe(), currentOutroText As String)
        IntroImageXmlContent = IO.File.ReadAllText(introXmlPaths(0))
        EphFrameXmlContent = IO.File.ReadAllText(introXmlPaths(1))
        OutroImageXmlContent = IO.File.ReadAllText(introXmlPaths(2))
        Ephs = currentEphs
        CurrentDate = currentEpheDate
        OutroText = currentOutroText.Trim()
        OutputDirectory = outDir
        WorkerBot = bot
    End Sub

    ''' <summary>
    ''' Genera las imágenes de las efemérides del día indicado.
    ''' </summary>
    Public Sub Generate()
        EventLogger.Log("Generando archivos .htm efemérides " & CurrentDate.ToString("dd/MM/yy"), "Generate")
        GenerateHtmlDesc()
        EventLogger.Log("Cargando logo de intro efemérides " & CurrentDate.ToString("dd/MM/yy"), "Generate")
        LoadIntroLogo()
        EventLogger.Log("Generando intro efemérides " & CurrentDate.ToString("dd/MM/yy"), "Generate")
        GenerateIntro(40)
        Dim ephq As Integer = CInt(Min(5, Ephs.Length))
        For i As Integer = 1 To ephq
            EventLogger.Log("Generando efemérides " & CurrentDate.ToString("dd/MM/yy") & " (" & i & " de " & ephq & ")", "Generate")
            GenerateEphe(310, Ephs(i - 1))
        Next
        EventLogger.Log("Generando outro efemérides " & CurrentDate.ToString("dd/MM/yy"), "Generate")
        GenerateOutro(80)
        Encode(OutputDirectory, CurrentDate)
    End Sub

    Sub GenerateHtmlDesc()

        Dim currentDateString As String = CurrentDate.Year.ToString & CurrentDate.Month.ToString("00") & CurrentDate.Day.ToString("00")
        Dim Fecha As String = CurrentDate.ToString("d 'de' MMMM", New Globalization.CultureInfo("es-ES"))
        Dim descriptionHtmlFilePath As String = ResultDir & currentDateString & ".htm"
        Dim musicDescriptionFilePath As String = ResultDir & currentDateString & ".txt"

        Dim headerFilePath As String = ResourcesDir & "header.hres"
        Dim bottomFilePath As String = ResourcesDir & "bottom.hres"



        If Not IO.File.Exists(headerFilePath) Then
            IO.File.Create(headerFilePath).Close()
        End If
        If Not IO.File.Exists(bottomFilePath) Then
            IO.File.Create(bottomFilePath).Close()
        End If

        If Not IO.File.Exists(descriptionHtmlFilePath) Then
            IO.File.Create(descriptionHtmlFilePath).Close()
        End If

        If Not IO.File.Exists(musicDescriptionFilePath) Then
            IO.File.Create(musicDescriptionFilePath).Close()
        End If

        Dim htext As String = IO.File.ReadAllText(headerFilePath)
        Dim btext As String = IO.File.ReadAllText(bottomFilePath)


        Dim efeinfotext As String = htext & CType(SettingsProvider.Get("VID_NAME_1"), String) & Fecha & CType(SettingsProvider.Get("VID_NAME_2"), String) & Environment.NewLine
        efeinfotext &= Environment.NewLine & "Enlaces:"

        For Each ef As WikiEphe In Ephs
            Dim t As String = "http://es.wikipedia.org/wiki/" & UrlWebEncode(ef.Page.Replace(" "c, "_"c))
            't = WorkerBot.GetShortenMetaWikiUrl(t)
            efeinfotext &= Environment.NewLine & "• "
            efeinfotext &= " " & ef.Page & ": "
            efeinfotext &= t
        Next
        efeinfotext = efeinfotext & Environment.NewLine & IO.File.ReadAllText(musicDescriptionFilePath, System.Text.Encoding.UTF8)
        efeinfotext = efeinfotext & btext
        IO.File.WriteAllText(descriptionHtmlFilePath, efeinfotext)
    End Sub

    Sub LoadIntroLogo()
        Dim intrologopath As String = CType(SettingsProvider.Get("INTRO_LOGO"), String)
        If IO.File.Exists(intrologopath) Then
            Dim timg As Image = Image.FromFile(intrologopath)
            Dim imgdata As String = ImageToBase64String(timg)
            IntroImageXmlContent = IntroImageXmlContent.Replace("LOGO_DATA", imgdata)
        End If
    End Sub

    Function Encode(ByVal tpath As String, tdate As Date) As Boolean
        EventLogger.Log("Generando video", "EncodeVideo")
        Dim tdatestring As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
        Dim MusicFile As String = ResultDir & tdatestring & ".mp3"
        Try
            EventLogger.Log("Llamando a ffmpeg", "EncodeVideo")
            Using exec As New Process
                exec.StartInfo.FileName = "ffmpeg"
                exec.StartInfo.UseShellExecute = True
                If IO.File.Exists(MusicFile) Then
                    exec.StartInfo.Arguments = "-y -r 29 -i """ & tpath & "ef_%04d.svg""" & " -i """ & MusicFile & """ -vcodec libx264 -preset slower -crf 19 -pix_fmt yuv420p -shortest -strict -2 """ & ResultDir & tdatestring & ".mp4"""
                Else
                    exec.StartInfo.Arguments = "-y -r 29 -i """ & tpath & "ef_%04d.svg""" & " -vcodec libx264 -preset slower -crf 19 -pix_fmt yuv420p -shortest -strict -2 """ & ResultDir & tdatestring & ".mp4"""
                End If
                exec.Start()
                exec.WaitForExit()
            End Using
            Dim dir As New DirectoryInfo(TempDir)
            EventLogger.Log("Eliminando archivos temporales", "EncodeVideo")
            For Each file As FileInfo In dir.EnumerateFiles()
                file.Delete()
            Next
        Catch ex As SystemException
            EventLogger.EX_Log("EX Encoding: " & ex.Message, "EncodeVideo")
            Return False
        End Try
        Return True
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="frames">Mínimo recomendado: 10 frames. Valores deben ser múltiplos de 10.</param>
    ''' <returns></returns>
    Private Function GenerateIntro(ByVal frames As Integer) As Integer
        Dim fadeInFrames As Integer = (frames \ 10) * 2
        Dim staticIntroFrames As Integer = (frames \ 10) * 6
        Dim fadeOutFrames As Integer = (frames \ 10) * 2


        For currentFrame As Integer = 1 To fadeInFrames
            EffectiveFrames += 1
            Dim currentFrameXmlContent As String = IntroImageXmlContent
            Dim opacity As Double = LinealScaleToLog(fadeInFrames, currentFrame)
            Dim frameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            currentFrameXmlContent = currentFrameXmlContent.Replace("fill-opacity=""0.000""", "fill-opacity=""" & opacity & """")
            currentFrameXmlContent = currentFrameXmlContent.Replace("XX de XXXXXX", EpheProvider.GetDateAsSpaString(CurrentDate))
            SvgTextImageFile(frameFileName, currentFrameXmlContent)

        Next

        For currentFrame As Integer = 1 To staticIntroFrames
            EffectiveFrames += 1
            Dim currentFrameXmlContent As String = IntroImageXmlContent
            Dim frameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            currentFrameXmlContent = currentFrameXmlContent.Replace("XX de XXXXXX", EpheProvider.GetDateAsSpaString(CurrentDate))
            SvgTextImageFile(frameFileName, currentFrameXmlContent)
        Next

        For currentFrame As Integer = 1 To fadeOutFrames
            EffectiveFrames += 1
            Dim currentFrameXmlContent As String = IntroImageXmlContent
            Dim opacity As Double = 1 - LinealScaleToLog(fadeOutFrames, currentFrame)
            Dim frameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            currentFrameXmlContent = currentFrameXmlContent.Replace("fill-opacity=""0.000""", "fill-opacity=""" & opacity & """")
            currentFrameXmlContent = currentFrameXmlContent.Replace("XX de XXXXXX", EpheProvider.GetDateAsSpaString(CurrentDate))
            SvgTextImageFile(frameFileName, currentFrameXmlContent)
        Next
        Return EffectiveFrames
    End Function

    Private Function GenerateEphe(ByVal frames As Integer, eph As WikiEphe) As Integer
        Dim fadeInFrames As Integer = (frames \ 10) * 2
        Dim textFrames = (frames \ 10) * 2
        Dim staticFrames As Integer = (frames \ 10) * 5
        Dim fadeOutFrames As Integer = (frames \ 10)

        Dim ephBaseSvg As String = GenerateEPhBaseSvg(eph)
        Dim ephLastSvg As String = ephBaseSvg

        Dim fadein_pre As Integer = fadeInFrames \ 4
        Dim fadein_img As Integer = fadeInFrames \ 2
        Dim fadein_2 As Integer = fadeInFrames \ 4


        For i As Integer = 1 To fadein_pre
            EffectiveFrames += 1
            Dim currentFrameSVG As String = ephBaseSvg
            Dim opacity As Double = LinealScaleToLog(fadein_pre, i)
            currentFrameSVG = currentFrameSVG.Replace("fill-opacity=""00.000""", "fill-opacity=""" & opacity.ToString() & """")
            currentFrameSVG = currentFrameSVG.Replace("fill-opacity=""0.800""", "fill-opacity=""0""")
            currentFrameSVG = currentFrameSVG.Replace("<svg x=""0"" y=""540px""", "<svg x=""1200px"" y=""540px""")
            currentFrameSVG = Regex.Replace(currentFrameSVG, "<text x=""3[0-9]{2}px""", "<text x=""1200px""")
            currentFrameSVG = currentFrameSVG.Replace("x=""10""", "x=""1200px""")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            ephLastSvg = currentFrameSVG
            SvgTextImageFile(currentFrameFileName, currentFrameSVG)
        Next

        For i As Integer = 1 To fadein_img
            EffectiveFrames += 1
            Dim currentFrameSVG As String = ephBaseSvg
            currentFrameSVG = currentFrameSVG.Replace("fill-opacity=""00.000""", "fill-opacity=""0.000""")
            currentFrameSVG = currentFrameSVG.Replace("fill-opacity=""0.800""", "fill-opacity=""0.000000""")
            currentFrameSVG = currentFrameSVG.Replace("<svg x=""0"" y=""540px""", "<svg x=""1200px"" y=""540px""")
            currentFrameSVG = Regex.Replace(currentFrameSVG, "<text x=""3[0-9]{2}px""", "<text x=""1200px""")
            currentFrameSVG = currentFrameSVG.Replace("x=""10""", "x=""1201px""")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            ephLastSvg = currentFrameSVG
            SvgTextImageFile(currentFrameFileName, currentFrameSVG)
        Next

        For i As Integer = 1 To fadein_2
            EffectiveFrames += 1
            Dim x_coord_year As Double = LinealScaleToLog(fadein_2, i) * 600
            Dim currentFrameSVG As String = ephLastSvg
            Dim opacity As Double = 1 - LinealScaleToLog(fadein_2, i)
            Dim bgopacity As Double = 1 - LinealScaleToLog(fadein_2, i)
            Dim blackBarOpacity As Double = LinealScaleToLog(fadein_2, i)
            If bgopacity > 0.45 Then
                bgopacity = 0.45
            End If
            currentFrameSVG = currentFrameSVG.Replace("fill-opacity=""0.000000""", "fill-opacity=""" & bgopacity & """")
            currentFrameSVG = currentFrameSVG.Replace("fill-opacity=""00.000""", "fill-opacity=""" & blackBarOpacity.ToString() & """")
            currentFrameSVG = currentFrameSVG.Replace("x=""1201px""", "x=""" & (x_coord_year + 10).ToString & """")
            currentFrameSVG = currentFrameSVG.Replace("<svg x=""1200px"" y=""540px""", "<svg x=""0"" y=""540px""")
            currentFrameSVG = currentFrameSVG.Replace("opacity=""1.0000""", "opacity=""" & opacity & """")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameSVG)
        Next

        For i As Integer = 1 To textFrames
            EffectiveFrames += 1
            Dim currentFrameSVG As String = ephBaseSvg
            Dim currX As Double = (LinealScaleToLog(textFrames, i) * -1200) + 300

            For f As Integer = 300 To 320
                currentFrameSVG = currentFrameSVG.Replace("<text x=""" & f & "px""", "<text x=""" & currX & "px""")
            Next
            currentFrameSVG = currentFrameSVG.Replace("fill-opacity=""0.800""", "fill-opacity=""0.45""")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameSVG)
        Next

        For i As Integer = 1 To staticFrames
            EffectiveFrames += 1
            Dim currentFrameSVG As String = ephBaseSvg
            currentFrameSVG = currentFrameSVG.Replace("fill-opacity=""0.800""", "fill-opacity=""0.45""")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameSVG)
        Next

        For i As Integer = 1 To fadeOutFrames
            EffectiveFrames += 1
            Dim currentFrameSVG As String = ephBaseSvg
            Dim opacity As Double = 1 - LinealScaleToLog(fadeOutFrames, i)
            currentFrameSVG = currentFrameSVG.Replace("fill-opacity=""0.800""", "fill-opacity=""0.45""")
            currentFrameSVG = currentFrameSVG.Replace("fill-opacity=""00.000""", "fill-opacity=""" & opacity.ToString() & """")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameSVG)
        Next

        Return EffectiveFrames
    End Function

    Private Function GenerateOutro(ByVal frames As Integer) As Integer
        Dim ephline As String = "<text x=""300px"" y=""SEPARATION"" text-anchor=""middle"" font-family=""Verdana"" font-size=""15"" fill=""#ebebf0"" lengthAdjust=""spacing"">TEXT_LINE</text>"
        Dim fadein_frames As Integer = frames \ 2


        For i As Integer = 1 To fadein_frames
            EffectiveFrames += 1
            Dim currentFrameXmlContent As String = OutroImageXmlContent
            Dim opacity As Double = LinealScaleToLog(fadein_frames, i)
            currentFrameXmlContent = currentFrameXmlContent.Replace("fill=""white"" fill-opacity=""0""", "fill=""white"" fill-opacity="" " & opacity.ToString() & """")
            Dim lines As String() = OutroText.Split(Environment.NewLine)
            For b As Integer = 0 To lines.Length - 1
                Dim l As String = lines(b).Trim()
                Dim currentline As String = ephline.Replace("TEXT_LINE", l)
                currentline = currentline.Replace("SEPARATION", ((500 / lines.Length) * (b + 1)).ToString)
                currentFrameXmlContent = currentFrameXmlContent.Replace("<!--TEXT_LINE-->", currentline & Environment.NewLine & "<!--TEXT_LINE-->")
            Next
            currentFrameXmlContent = currentFrameXmlContent.Replace("<!--TEXT_LINE-->", "")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameXmlContent)
        Next

        For fr As Integer = 1 To fadein_frames
            EffectiveFrames += 1
            Dim currentFrameXmlContent As String = OutroImageXmlContent
            Dim lines As String() = OutroText.Split(Environment.NewLine)
            For b As Integer = 0 To lines.Length - 1
                Dim l As String = lines(b).Trim()
                Dim currentline As String = ephline.Replace("TEXT_LINE", l)
                currentline = currentline.Replace("SEPARATION", ((500 / lines.Length) * (b + 1)).ToString)
                currentFrameXmlContent = currentFrameXmlContent.Replace("<!--TEXT_LINE-->", currentline & Environment.NewLine & "<!--TEXT_LINE-->")
            Next
            currentFrameXmlContent = currentFrameXmlContent.Replace("<!--TEXT_LINE-->", "")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameXmlContent)
        Next

        Return EffectiveFrames
    End Function


    Private Function GenerateEPhBaseSvg(ByVal eph As WikiEphe) As String
        Dim currentFrameXmlContent As String = EphFrameXmlContent
        Dim imageData As Tuple(Of Image, String()) = GetCommonsFile("File:" & eph.Image)

        currentFrameXmlContent = AddYearToEphSvg(currentFrameXmlContent, eph.Year)
        currentFrameXmlContent = AddImageToEphSvg(currentFrameXmlContent, imageData.Item1)
        currentFrameXmlContent = AddLicensingToEphSvg(currentFrameXmlContent, eph.Image, imageData.Item2(2), imageData.Item2(0), imageData.Item2(1))
        currentFrameXmlContent = AddDescriptionToEphSvg(currentFrameXmlContent, eph.Description)

        Return currentFrameXmlContent
    End Function

    Private Function AddYearToEphSvg(ByVal svgString As String, ByVal year As Integer) As String
        Dim totalyears As Integer = CInt(Math.Truncate((CurrentDate - (New DateTime(year, CurrentDate.Month, CurrentDate.Day))).TotalDays / 365.2425))
        svgString = svgString.Replace("YEAR", year.ToString())
        svgString = svgString.Replace("TIMESPAN", "Hace " & totalyears.ToString() & " años")
        Return svgString
    End Function

    Private Function AddImageToEphSvg(ByVal svgString As String, ByVal commonsImage As Image) As String
        Dim imageAsBase64 As String = ImageToBase64String(commonsImage)
        svgString = svgString.Replace("IMAGE_DATA", imageAsBase64)
        Return svgString
    End Function

    Private Function AddLicensingToEphSvg(ByVal svgString As String, ByVal commonsName As String, commonsAuthor As String, commonsLicenseName As String, commonsLicenseURL As String) As String
        Dim currentFrameXmlContent As String = svgString
        Dim shortenedImageUrl As String = "https : //commons.wikimedia.org/wiki/File:" & commonsName 'WorkerBot.GetShortenMetaWikiUrl("https://commons.wikimedia.org/wiki/File:" & commonsName)
        Dim imageCommonsName As String = "Imagen en Wikimedia Commons: " & shortenedImageUrl
        Dim imageCommonsAuthor As String = "Autor de la imagen: " & commonsAuthor
        Dim imageCommonsLicense As String = "Licencia: " & commonsLicenseName & " (" & commonsLicenseURL & ")"

        If imageCommonsLicense.ToLower.Contains("dominio público") Or imageCommonsLicense.ToLower.Contains("dominio publico") Or imageCommonsLicense.ToLower.Contains("public domain") Then
            imageCommonsLicense = "Imagen en dominio público."
        End If

        currentFrameXmlContent = currentFrameXmlContent.Replace("COMMONS_URL", imageCommonsName)
        currentFrameXmlContent = currentFrameXmlContent.Replace("AUTHOR_NAME", imageCommonsAuthor)
        currentFrameXmlContent = currentFrameXmlContent.Replace("LICENSING", imageCommonsLicense)
        Return currentFrameXmlContent
    End Function

    Private Function AddDescriptionToEphSvg(ByVal svgString As String, ByVal descString As String) As String
        Dim ephline As String = "<text x=""300px"" y=""SEPARATION"" text-anchor=""middle"" font-family=""Verdana"" font-size=""TEXT_SIZE"" fill=""#ebebf0"" lengthAdjust=""spacing"">TEXT_LINE</text>"
        Dim ephchunks As String() = TrySplitText(descString, 30)
        Dim textScale As Tuple(Of Integer, Integer) = SelectTextScaleByChunks(ephchunks.Length)
        Dim textsize As Integer = textScale.Item1
        Dim textSeparation As Integer = textScale.Item2
        Dim currY As Integer = 200
        For i As Integer = 0 To ephchunks.Count - 1
            currY += textSeparation
            Dim lin As String = ephline.Replace("TEXT_SIZE", textsize.ToString())
            If i >= ephchunks.Count - 1 Then
                lin = lin.Replace(" lengthAdjust=""spacing""", " lengthAdjust=""none""")
            End If
            lin = lin.Replace("SEPARATION", currY.ToString())
            lin = lin.Replace("TEXT_LINE", ephchunks(i))
            lin = lin.Replace("x=""300px""", "x=""" & (300 + i).ToString & "px""")
            svgString = svgString.Replace("<!--TEXT_LINE-->", ControlChars.Tab & lin & Environment.NewLine & "<!--TEXT_LINE-->")
        Next
        Return svgString
    End Function

    Private Function SelectTextScaleByChunks(ByVal chunkAmount As Integer) As Tuple(Of Integer, Integer)
        Dim textsize As Integer
        Dim textSeparation As Integer

        Select Case chunkAmount
            Case 1
                textsize = 32
            Case 2
                textsize = 32
            Case 3
                textsize = 32
            Case 4
                textsize = 32
            Case 5
                textsize = 27
            Case 6
                textsize = 25
            Case Else
                textsize = 23
        End Select
        textSeparation = CInt(Math.Round(textsize * 1.5))
        Return New Tuple(Of Integer, Integer)(textsize, textSeparation)
    End Function


    Private Function LinealScaleToLog(maxVal As Double, currentVal As Double) As Double
        Return Min(Math.Log(maxVal / currentVal) / Math.E, 1)
    End Function

    Private Function Min(ByVal val1 As Double, val2 As Double) As Double
        If (val1 < val2) Then Return val1
        Return val2
    End Function

    Private Function Max(ByVal val1 As Double, val2 As Double) As Double
        If (val1 > val2) Then Return val1
        Return val2
    End Function

    Private Function SvgTextImageFile(ByVal imagePath As String, ByVal svgContent As String) As Boolean
        IO.File.WriteAllText(imagePath, svgContent)
        Return True
    End Function


    Private Function PicFromUrl(ByVal url As String, ByVal retries As Integer) As Image
        Dim img As Drawing.Image = New Bitmap(1, 1)

        For i As Integer = 0 To retries
            Try
                Using client As System.Net.Http.HttpClient = New Http.HttpClient()
                    client.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgent)
                    Using stream = client.GetStreamAsync(url).Result()
                        img = CType(Drawing.Image.FromStream(stream).Clone, Image)
                    End Using
                End Using
                Return img
            Catch ex As Exception
                img = New Bitmap(1, 1)
            End Try
        Next
        img.Dispose()
        EventLogger.EX_Log("Problemas al obtener una imagen desde Commons. URL al recurso:" & url, "PicFromUrl")
        Throw New MWBot.net.MaxRetriesExeption
    End Function

    Private Function GetCommonsFile(ByVal CommonsFilename As String) As Tuple(Of Image, String())
        Dim responsestring As String = NormalizeUnicodetext(WorkerBot.GETQUERY("action=query&format=json&titles=" & UrlWebEncode(CommonsFilename) & "&prop=imageinfo&iiprop=extmetadata|url&iiurlwidth=500"))
        Dim thumburlmatches As String() = TextInBetween(responsestring, """thumburl"":""", """,")
        Dim licencematches As String() = TextInBetween(responsestring, """LicenseShortName"":{""value"":""", """,")
        Dim licenceurlmatches As String() = TextInBetween(responsestring, """LicenseUrl"":{""value"":""", """,")
        Dim authormatches As String() = TextInBetween(responsestring, """Artist"":{""value"":""", """,")
        Dim matchstring As String = "<[\S\s]+?>"
        Dim matchstring2 As String = "\([\S\s]+?\)"

        Dim licence As String = String.Empty
        Dim licenceurl As String = String.Empty
        Dim author As String = String.Empty

        If licencematches.Count > 0 Then
            licence = Regex.Replace(licencematches(0), matchstring, "")
        End If
        If licenceurlmatches.Count > 0 Then
            licenceurl = Regex.Replace(licenceurlmatches(0), matchstring, "")
        End If
        If authormatches.Count > 0 Then
            author = Regex.Replace(authormatches(0), matchstring, "")
            author = Regex.Replace(author, matchstring2, "").Trim
            Dim authors As String() = author.Split(CType(Environment.NewLine, Char()))
            If authors.Count > 1 Then
                For i As Integer = 0 To authors.Count - 1
                    If Not String.IsNullOrWhiteSpace(authors(i)) Then
                        author = authors(i)
                    End If
                Next
            End If
            If author.Contains(":") Then
                author = author.Split(":"c)(1).Trim
            End If
        End If
        Dim img As Image = New Bitmap(1, 1)
        If thumburlmatches.Count > 0 Then
            img = PicFromUrl(thumburlmatches(0), 6)
        End If
        If String.IsNullOrWhiteSpace(author) Or (author.ToLower.Contains("unknown")) Then
            author = "Desconocido"
        End If
        Return New Tuple(Of Image, String())(img, {licence, licenceurl, author})
    End Function

    Private Function ImageToBase64String(ByVal img As Image) As String
        Dim str As String = String.Empty
        Using mem As New MemoryStream()
            img.Save(mem, ImageFormat.Png)
            mem.Position = 0
            Dim bBuffer As Byte() = mem.ToArray
            str = Convert.ToBase64String(bBuffer)
            bBuffer = Nothing
        End Using
        Return str
    End Function

    Private Function TrySplitText(ByVal str As String, limit As Integer) As String()
        Dim strl As New List(Of String)

        If (str.Contains(Environment.NewLine)) Then
            strl = str.Split(Environment.NewLine).ToList
        Else
            Dim words As String() = str.Split(" "c)
            Dim tmp As String = String.Empty
            Dim ch As Integer = 0
            For i As Integer = 0 To words.Length - 1
                tmp &= words(i) & " "
                ch = tmp.Length
                If ch >= limit OrElse (i = words.Length - 1 Or words(i).Contains(",") Or words(i).Contains(".") Or words(i).Contains(":") Or words(i).Contains(";")) Then
                    strl.Add(tmp.Substring(0, ch).Trim)
                    tmp = String.Empty
                End If
            Next
        End If

        Return strl.ToArray
    End Function


End Class
