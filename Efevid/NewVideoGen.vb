Option Strict On
Option Explicit On
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Efevid.Ephe
Imports MWBot.net.Utility.Utils
Imports SixLabors.ImageSharp
Imports SixLabors.ImageSharp.Formats.Png
Imports Image = SixLabors.ImageSharp.Image

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
    Private LastRequestDate As DateTime = DateTime.Now
    Private RateLimitSeconds As Double = 1.2

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
            't = WorkerBot.GetShortenMetaWikiUrl(t) 'GetShortenMetaWikiUrl requires permissions on meta
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
            Dim timg As Image = Image.Load(intrologopath)
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
    ''' Generates frames for an introductory sequence with fade-in, static display, and fade-out effects.
    ''' This function creates SVG images for each frame of the intro animation.
    ''' </summary>
    ''' <param name="totalFrames">The total number of frames for the entire intro. 
    ''' Recommended minimum: 10 frames. Values should be multiples of 10 for proper timing.</param>
    ''' <returns>The number of effective frames generated.</returns>
    Private Function GenerateIntro(ByVal totalFrames As Integer) As Integer
        ' Define constants for the duration of each phase of the intro in terms of frame segments
        Const fadeInDuration As Integer = 2 ' Fade-in phase lasts for 2 segments of total frames divided by 10
        Const staticIntroDuration As Integer = 6 ' Static phase lasts for 6 segments
        Const fadeOutDuration As Integer = 2 ' Fade-out phase lasts for 2 segments

        ' Calculate the number of frames for each phase based on totalFrames
        Dim fadeInFrames As Integer = CInt((totalFrames / 10) * fadeInDuration)
        Dim staticIntroFrames As Integer = CInt((totalFrames / 10) * staticIntroDuration)
        Dim fadeOutFrames As Integer = CInt((totalFrames / 10) * fadeOutDuration)

        ' Loop to generate frames for the fade-in effect
        For currentFrame As Integer = 1 To fadeInFrames
            EffectiveFrames += 1
            ' Start with base XML content for intro image
            Dim currentFrameXmlContent As String = IntroImageXmlContent
            ' Calculate opacity for linear to logarithmic scale transition
            Dim opacity As Double = LinealScaleToLog(fadeInFrames, currentFrame)
            ' Generate unique filename for each frame
            Dim frameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            ' Update opacity in XML content
            currentFrameXmlContent = currentFrameXmlContent.Replace("fill-opacity=""0.000""", "fill-opacity=""" & opacity & """")
            ' Replace placeholder date with actual date
            currentFrameXmlContent = currentFrameXmlContent.Replace("XX de XXXXXX", EpheProvider.GetDateAsSpaString(CurrentDate))
            ' Write SVG content to file
            SvgTextImageFile(frameFileName, currentFrameXmlContent)
        Next

        ' Loop to generate static frames after fade-in
        For currentFrame As Integer = 1 To staticIntroFrames
            EffectiveFrames += 1
            Dim currentFrameXmlContent As String = IntroImageXmlContent
            Dim frameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            ' No need to change opacity since this is the static phase
            currentFrameXmlContent = currentFrameXmlContent.Replace("XX de XXXXXX", EpheProvider.GetDateAsSpaString(CurrentDate))
            SvgTextImageFile(frameFileName, currentFrameXmlContent)
        Next

        ' Loop to generate frames for the fade-out effect
        For currentFrame As Integer = 1 To fadeOutFrames
            EffectiveFrames += 1
            Dim currentFrameXmlContent As String = IntroImageXmlContent
            ' Calculate decreasing opacity for fade-out
            Dim opacity As Double = 1 - LinealScaleToLog(fadeOutFrames, currentFrame)
            Dim frameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            currentFrameXmlContent = currentFrameXmlContent.Replace("fill-opacity=""0.000""", "fill-opacity=""" & opacity & """")
            currentFrameXmlContent = currentFrameXmlContent.Replace("XX de XXXXXX", EpheProvider.GetDateAsSpaString(CurrentDate))
            SvgTextImageFile(frameFileName, currentFrameXmlContent)
        Next

        ' Return the total number of frames generated
        Return EffectiveFrames
    End Function

    ''' <summary>
    ''' Generates frames for an ephe (presumably ephemeris) sequence with multiple phases including fade-in, text movement, static display, and fade-out.
    ''' This function manipulates SVG content to create animations for displaying ephemeris data.
    ''' </summary>
    ''' <param name="totalFrames">The total number of frames for the entire animation sequence. 
    ''' Values should be multiples of 10 for proper timing.</param>
    ''' <param name="eph">An instance of WikiEphe, which contains the ephemeris data to be displayed.</param>
    ''' <returns>The number of effective frames generated.</returns>
    Private Function GenerateEphe(ByVal totalFrames As Integer, eph As WikiEphe) As Integer
        ' Define constants for the duration of each animation phase in terms of frame segments
        Const fadeInDuration As Integer = 2 ' Fade-in phase lasts for 2 segments
        Const textDuration As Integer = 2 ' Text movement phase lasts for 2 segments
        Const staticDuration As Integer = 5 ' Static display phase lasts for 5 segments
        Const fadeOutDuration As Integer = 1 ' Fade-out phase lasts for 1 segment

        ' Calculate the number of frames for each phase based on totalFrames
        Dim fadeInFrames As Integer = CInt((totalFrames / 10) * fadeInDuration)
        Dim textFrames As Integer = CInt((totalFrames / 10) * textDuration)
        Dim staticFrames As Integer = CInt((totalFrames / 10) * staticDuration)
        Dim fadeOutFrames As Integer = CInt((totalFrames / 10) * fadeOutDuration)

        ' Generate the base SVG code for the ephe
        Dim baseSvgCode As String = GenerateEPhBaseSvg(eph)
        Dim lastSvgCode As String = baseSvgCode

        ' Divide the fade-in phase into three sub-phases for different effects
        Dim fadeInPreFrames As Integer = fadeInFrames \ 4
        Dim fadeInImageFrames As Integer = fadeInFrames \ 2
        Dim fadeIn2Frames As Integer = fadeInFrames \ 4

        ' First part of fade-in: Move elements off-screen with opacity change
        For i As Integer = 1 To fadeInPreFrames
            EffectiveFrames += 1
            Dim currentFrameSvgCode As String = baseSvgCode
            Dim opacity As Double = LinealScaleToLog(fadeInPreFrames, i)
            currentFrameSvgCode = currentFrameSvgCode.Replace("fill-opacity=""00.000""", $"fill-opacity=""{opacity}""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("fill-opacity=""0.800""", "fill-opacity=""0""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("<svg x=""0"" y=""540px""", "<svg x=""1200px"" y=""540px""")
            currentFrameSvgCode = Regex.Replace(currentFrameSvgCode, "<text x=""3[0-9]{2}px""", "<text x=""1200px""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("x=""10""", "x=""1200px""")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            lastSvgCode = currentFrameSvgCode
            SvgTextImageFile(currentFrameFileName, currentFrameSvgCode)
        Next

        ' Second part of fade-in: Keep elements off-screen but adjust opacity
        For i As Integer = 1 To fadeInImageFrames
            EffectiveFrames += 1
            Dim currentFrameSvgCode As String = baseSvgCode
            currentFrameSvgCode = currentFrameSvgCode.Replace("fill-opacity=""00.000""", "fill-opacity=""0.000""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("fill-opacity=""0.800""", "fill-opacity=""0.000000""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("<svg x=""0"" y=""540px""", "<svg x=""1200px"" y=""540px""")
            currentFrameSvgCode = Regex.Replace(currentFrameSvgCode, "<text x=""3[0-9]{2}px""", "<text x=""1200px""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("x=""10""", "x=""1201px""")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            lastSvgCode = currentFrameSvgCode
            SvgTextImageFile(currentFrameFileName, currentFrameSvgCode)
        Next

        ' Third part of fade-in: Transition elements back on-screen with opacity adjustments
        For i As Integer = 1 To fadeIn2Frames
            EffectiveFrames += 1
            Dim xCoordYear As Double = LinealScaleToLog(fadeIn2Frames, i) * 600
            Dim currentFrameSvgCode As String = lastSvgCode
            Dim opacity As Double = 1 - LinealScaleToLog(fadeIn2Frames, i)
            Dim bgOpacity As Double = 1 - LinealScaleToLog(fadeIn2Frames, i)
            Dim blackBarOpacity As Double = LinealScaleToLog(fadeIn2Frames, i)
            If bgOpacity > 0.45 Then bgOpacity = 0.45
            currentFrameSvgCode = currentFrameSvgCode.Replace("fill-opacity=""0.000000""", $"fill-opacity=""{bgOpacity}""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("fill-opacity=""00.000""", $"fill-opacity=""{blackBarOpacity}""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("x=""1201px""", $"x=""{xCoordYear + 10}""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("<svg x=""1200px"" y=""540px""", "<svg x=""0"" y=""540px""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("opacity=""1.0000""", $"opacity=""{opacity}""")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameSvgCode)
        Next

        ' Text movement phase: Slide text from right to left
        For i As Integer = 1 To textFrames
            EffectiveFrames += 1
            Dim currentFrameSvgCode As String = baseSvgCode
            Dim currX As Double = (LinealScaleToLog(textFrames, i) * -1200) + 300

            For f As Integer = 300 To 320
                currentFrameSvgCode = currentFrameSvgCode.Replace("<text x=""" & f & "px""", "<text x=""" & currX & "px""")
            Next
            currentFrameSvgCode = currentFrameSvgCode.Replace("fill-opacity=""0.800""", "fill-opacity=""0.45""")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameSvgCode)
        Next

        ' Static display phase: Show content without changes
        For i As Integer = 1 To staticFrames
            EffectiveFrames += 1
            Dim currentFrameSvgCode As String = baseSvgCode
            currentFrameSvgCode = currentFrameSvgCode.Replace("fill-opacity=""0.800""", "fill-opacity=""0.45""")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameSvgCode)
        Next

        ' Fade-out phase: Gradually decrease opacity of elements
        For i As Integer = 1 To fadeOutFrames
            EffectiveFrames += 1
            Dim currentFrameSvgCode As String = baseSvgCode
            Dim opacity As Double = 1 - LinealScaleToLog(fadeOutFrames, i)
            currentFrameSvgCode = currentFrameSvgCode.Replace("fill-opacity=""0.800""", "fill-opacity=""0.45""")
            currentFrameSvgCode = currentFrameSvgCode.Replace("fill-opacity=""00.000""", $"fill-opacity=""{opacity}""")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameSvgCode)
        Next

        Return EffectiveFrames
    End Function

    ''' <summary>
    ''' Generates frames for an outro sequence with a fade-in effect for text lines.
    ''' This function creates SVG images for each frame of the outro animation, displaying multiple lines of text.
    ''' </summary>
    ''' <param name="totalFrames">The total number of frames for the entire outro. 
    ''' The function uses half of these for fade-in and the other half for static display.</param>
    ''' <returns>The number of effective frames generated.</returns>
    Private Function GenerateOutro(ByVal totalFrames As Integer) As Integer
        ' Define duration for the fade-in phase in terms of frame segments
        Const fadeInDuration As Integer = 2 ' Fade-in phase lasts for 2 segments of total frames divided by 2

        ' Template for each text line in SVG format
        Dim ephlineTemplate As String = "<text x=""300px"" y=""SEPARATION"" text-anchor=""middle"" font-family=""Verdana"" font-size=""15"" fill=""#ebebf0"" lengthAdjust=""spacing"">TEXT_LINE</text>"
        ' Calculate the number of frames for the fade-in phase
        Dim fadeInFrames As Integer = CInt((totalFrames / 2) * fadeInDuration)

        ' First loop for fade-in effect where text opacity increases
        For i As Integer = 1 To fadeInFrames
            EffectiveFrames += 1
            ' Start with base XML content for outro image
            Dim currentFrameXmlContent As String = OutroImageXmlContent
            ' Calculate opacity for linear to logarithmic scale transition
            Dim opacity As Double = LinealScaleToLog(fadeInFrames, i)

            ' Adjust the opacity of text in the SVG
            currentFrameXmlContent = currentFrameXmlContent.Replace("fill=""white"" fill-opacity=""0""", $"fill=""white"" fill-opacity=""{opacity}""")

            ' Split the outro text into lines
            Dim lines As String() = OutroText.Split(Environment.NewLine)

            For b As Integer = 0 To lines.Length - 1
                ' Trim the line to remove any leading/trailing whitespace
                Dim line As String = lines(b).Trim()
                ' Prepare the line for SVG by replacing placeholders
                Dim ephline As String = ephlineTemplate.Replace("TEXT_LINE", line).Replace("SEPARATION", ((500 / lines.Length) * (b + 1)).ToString())
                ' Insert the line into the SVG content at the placeholder
                currentFrameXmlContent = currentFrameXmlContent.Replace("<!--TEXT_LINE-->", ephline & Environment.NewLine & "<!--TEXT_LINE-->")
            Next

            ' Remove the placeholder after all lines are inserted
            currentFrameXmlContent = currentFrameXmlContent.Replace("<!--TEXT_LINE-->", "")
            ' Generate filename for the current frame
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            ' Save the modified SVG content to file
            SvgTextImageFile(currentFrameFileName, currentFrameXmlContent)
        Next

        ' Second loop for static display phase where text remains at full opacity
        For i As Integer = 1 To fadeInFrames
            EffectiveFrames += 1
            Dim currentFrameXmlContent As String = OutroImageXmlContent
            Dim lines As String() = OutroText.Split(Environment.NewLine)

            For b As Integer = 0 To lines.Length - 1
                Dim line As String = lines(b).Trim()
                Dim ephline As String = ephlineTemplate.Replace("TEXT_LINE", line).Replace("SEPARATION", ((500 / lines.Length) * (b + 1)).ToString())
                currentFrameXmlContent = currentFrameXmlContent.Replace("<!--TEXT_LINE-->", ephline & Environment.NewLine & "<!--TEXT_LINE-->")
            Next

            currentFrameXmlContent = currentFrameXmlContent.Replace("<!--TEXT_LINE-->", "")
            Dim currentFrameFileName As String = OutputDirectory & "ef_" & EffectiveFrames.ToString("0000") & ".svg"
            SvgTextImageFile(currentFrameFileName, currentFrameXmlContent)
        Next

        ' Return the total number of frames generated
        Return EffectiveFrames
    End Function

    ''' <summary>
    ''' Generates the base SVG content for an ephemeris frame.
    ''' </summary>
    ''' <param name="eph">The WikiEphe object containing ephemeris data.</param>
    ''' <returns>A string containing the modified SVG content.</returns>
    Private Function GenerateEPhBaseSvg(ByVal eph As WikiEphe) As String
        Dim currentFrameXmlContent As String = EphFrameXmlContent
        Dim imageData As Tuple(Of Image, String()) = GetCommonsFile("File:" & eph.Image)

        currentFrameXmlContent = AddYearToEphSvg(currentFrameXmlContent, eph.Year)
        currentFrameXmlContent = AddImageToEphSvg(currentFrameXmlContent, imageData.Item1)
        currentFrameXmlContent = AddLicensingToEphSvg(currentFrameXmlContent, eph.Image, imageData.Item2(2), imageData.Item2(0), imageData.Item2(1))
        currentFrameXmlContent = AddDescriptionToEphSvg(currentFrameXmlContent, eph.Description)

        Return currentFrameXmlContent
    End Function

    ''' <summary>
    ''' Adds year information to the SVG string.
    ''' </summary>
    ''' <param name="svgString">Original SVG content.</param>
    ''' <param name="year">The year to display.</param>
    ''' <returns>Updated SVG string with year information.</returns>
    Private Function AddYearToEphSvg(ByVal svgString As String, ByVal year As Integer) As String
        ' Calculate years elapsed from the given year to the current date
        Dim totalyears As Integer = CInt(Math.Truncate((CurrentDate - New DateTime(year, CurrentDate.Month, CurrentDate.Day)).TotalDays / 365.2425))
        svgString = svgString.Replace("YEAR", year.ToString())
        svgString = svgString.Replace("TIMESPAN", "Hace " & totalyears.ToString() & " años")
        Return svgString
    End Function

    ''' <summary>
    ''' Adds an image as base64 to the SVG string.
    ''' </summary>
    ''' <param name="svgString">Original SVG content.</param>
    ''' <param name="commonsImage">The Image object to embed.</param>
    ''' <returns>Updated SVG string with embedded image.</returns>
    Private Function AddImageToEphSvg(ByVal svgString As String, ByVal commonsImage As Image) As String
        Dim imageAsBase64 As String = ImageToBase64String(commonsImage)
        svgString = svgString.Replace("IMAGE_DATA", imageAsBase64)
        Return svgString
    End Function

    ''' <summary>
    ''' Adds licensing information to the SVG string.
    ''' </summary>
    ''' <param name="svgString">Original SVG content.</param>
    ''' <param name="commonsName">Name of the image file on Commons.</param>
    ''' <param name="commonsAuthor">The author of the image.</param>
    ''' <param name="commonsLicenseName">The name of the license.</param>
    ''' <param name="commonsLicenseURL">URL to the license information.</param>
    ''' <returns>Updated SVG string with licensing information.</returns>
    Private Function AddLicensingToEphSvg(ByVal svgString As String, ByVal commonsName As String, commonsAuthor As String, commonsLicenseName As String, commonsLicenseURL As String) As String
        Dim fullImageUrl As String = "https://commons.wikimedia.org/wiki/File:" & commonsName
        Dim shortenedImageUrl As String = fullImageUrl 'WorkerBot.GetShortenMetaWikiUrl(fullImageUrl) Requires special permissions in Meta Wiki.

        ' Check if the shortened URL is valid
        If String.IsNullOrEmpty(shortenedImageUrl) Then
            shortenedImageUrl = fullImageUrl ' Fallback to the full URL if shortening fails
        End If

        Dim imageCommonsName As String = "Imagen en Wikimedia Commons: " & shortenedImageUrl
        Dim imageCommonsAuthor As String = "Autor de la imagen: " & commonsAuthor
        Dim imageCommonsLicense As String = "Licencia: " & commonsLicenseName & " (" & commonsLicenseURL & ")"

        ' Check if the image is in the public domain
        If imageCommonsLicense.ToLower.Contains("dominio público") Or imageCommonsLicense.ToLower.Contains("dominio publico") Or imageCommonsLicense.ToLower.Contains("public domain") Then
            imageCommonsLicense = "Imagen en dominio público."
        End If

        svgString = svgString.Replace("COMMONS_URL", imageCommonsName)
        svgString = svgString.Replace("AUTHOR_NAME", imageCommonsAuthor)
        svgString = svgString.Replace("LICENSING", imageCommonsLicense)
        Return svgString
    End Function

    ''' <summary>
    ''' Adds a description to the SVG string, adjusting text size and position based on content length.
    ''' </summary>
    ''' <param name="svgString">Original SVG content.</param>
    ''' <param name="descString">The description text to add.</param>
    ''' <returns>Updated SVG string with description text.</returns>
    Private Function AddDescriptionToEphSvg(ByVal svgString As String, ByVal descString As String) As String
        Dim ephline As String = "<text x=""300px"" y=""SEPARATION"" text-anchor=""middle"" font-family=""Verdana"" font-size=""TEXT_SIZE"" fill=""#ebebf0"" lengthAdjust=""spacing"">TEXT_LINE</text>"
        Dim ephchunks As String() = TrySplitText(descString, 30)
        Dim textScale As Tuple(Of Integer, Integer) = SelectTextScaleByChunks(ephchunks.Length)
        Dim textsize As Integer = textScale.Item1
        Dim textSeparation As Integer = textScale.Item2
        Dim currY As Integer = 200

        For i As Integer = 0 To ephchunks.Length - 1 ' Changed to Length for consistency with array usage in VB.NET
            currY += textSeparation
            Dim lin As String = ephline.Replace("TEXT_SIZE", textsize.ToString())
            If i >= ephchunks.Length - 1 Then
                lin = lin.Replace(" lengthAdjust=""spacing""", " lengthAdjust=""none""")
            End If
            lin = lin.Replace("SEPARATION", currY.ToString())
            lin = lin.Replace("TEXT_LINE", ephchunks(i))
            ' Note: Adjusting x-coordinate might not achieve the intended effect for text alignment or spacing
            lin = lin.Replace("x=""300px""", "x=""" & (300 + i).ToString() & "px""")
            svgString = svgString.Replace("<!--TEXT_LINE-->", ControlChars.Tab & lin & Environment.NewLine & "<!--TEXT_LINE-->")
        Next
        Return svgString
    End Function

    ''' <summary>
    ''' Determines text size and line separation based on the number of text chunks.
    ''' </summary>
    ''' <param name="chunkAmount">Number of text chunks.</param>
    ''' <returns>A tuple containing text size and line separation.</returns>
    Private Function SelectTextScaleByChunks(ByVal chunkAmount As Integer) As Tuple(Of Integer, Integer)
        Dim textsize As Integer
        Dim textSeparation As Integer

        Select Case chunkAmount
            Case 1 To 4
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
        Dim img As Image = Nothing

        For i As Integer = 0 To retries - 1 ' Loop from 0 to retries - 1
            Try
                ' Calculate time since last request and wait if necessary
                Dim timeSinceLastRequest As TimeSpan = DateTime.Now - LastRequestDate
                If timeSinceLastRequest.TotalSeconds < RateLimitSeconds Then
                    Dim waitTime As Integer = CInt(Math.Ceiling(RateLimitSeconds - timeSinceLastRequest.TotalSeconds) * 1000)
                    Thread.Sleep(waitTime) ' Wait to respect rate limit
                End If

                Using stream As MemoryStream = WorkerBot.GetAsStream(url) ' Use Workerbot to get the stream
                    img = Image.Load(stream)
                    ' Update the last request time after a successful request
                    LastRequestDate = DateTime.Now
                    Return img ' Return immediately on success
                End Using
            Catch ex As HttpRequestException When i < retries - 1
                ' Log the network error but continue to retry if possible
                EventLogger.EX_Log($"Network error. Attempt {i + 1}/{retries}. URL: {url}. Error: {ex.Message}", "PicFromUrl")
            Catch ex As ImageFormatException When i < retries - 1
                ' Log the image format error but continue to retry if possible
                EventLogger.EX_Log($"Image format error. Attempt {i + 1}/{retries}. URL: {url}. Error: {ex.Message}", "PicFromUrl")
            Catch ex As Exception
                ' Log any other exceptions but only if this is the last retry attempt
                If i = retries - 1 Then
                    EventLogger.EX_Log($"Unexpected error on last attempt. URL: {url}. Error: {ex.Message}", "PicFromUrl")
                End If
            End Try
        Next

        ' If we've exhausted all retries, log the failure and throw an exception
        EventLogger.EX_Log($"Failed to retrieve image after {retries} attempts. URL: {url}", "PicFromUrl")
        Throw New MWBot.net.MaxRetriesExeption()
    End Function

    Private Function GetCommonsFile(ByVal CommonsFilename As String) As Tuple(Of Image, String())
        Dim responseString As String = NormalizeUnicodetext(WorkerBot.GETQUERY("action=query&format=json&titles=" & UrlWebEncode(CommonsFilename) & "&prop=imageinfo&iiprop=extmetadata|url&iiurlwidth=500"))
        Dim thumburlMatches As String() = TextInBetween(responseString, """thumburl"":""", """,")
        Dim licenceMatches As String() = TextInBetween(responseString, """LicenseShortName"":{""value"":""", """,")
        Dim licenceUrlMatches As String() = TextInBetween(responseString, """LicenseUrl"":{""value"":""", """,")
        Dim authorMatches As String() = TextInBetween(responseString, """Artist"":{""value"":""", """,")
        Dim matchString As String = "<[\S\s]+?>"
        Dim matchString2 As String = "\([\S\s]+?\)"

        Dim licence As String = String.Empty
        Dim licenceUrl As String = String.Empty
        Dim author As String = String.Empty

        If licenceMatches.Length > 0 Then
            licence = Regex.Replace(licenceMatches(0), matchString, "")
        End If
        If licenceUrlMatches.Length > 0 Then
            licenceUrl = Regex.Replace(licenceUrlMatches(0), matchString, "")
        End If
        If authorMatches.Length > 0 Then
            author = NormalizeAuthor(Regex.Replace(authorMatches(0), matchString, ""))
        End If

        Dim img As Image = Nothing ' Declare img as Nothing initially
        If thumburlMatches.Length > 0 Then
            img = PicFromUrl(thumburlMatches(0), 6)
        Else
            img = Image.Load(New MemoryStream()) ' Load an empty image if no thumburl found
        End If

        Return New Tuple(Of Image, String())(img, {licence, licenceUrl, author})
    End Function

    Private Function NormalizeAuthor(ByVal text As String) As String
        Dim matchString2 As String = "\([\S\s]+?\)"
        text = Regex.Replace(text, matchString2, "").Trim
        Dim authors As String() = text.Split(CType(Environment.NewLine, Char()))
        If authors.Length > 1 Then
            For i As Integer = 0 To authors.Length - 1
                If Not String.IsNullOrWhiteSpace(authors(i)) Then
                    text = authors(i)
                    Exit For
                End If
            Next
        End If
        If text.Contains(":") Then
            text = text.Split(":"c)(1).Trim
        End If
        If String.IsNullOrWhiteSpace(text) OrElse text.ToLower().Contains("unknown") Then
            text = "Desconocido"
        End If
        Return text
    End Function

    Private Function ImageToBase64String(ByVal img As Image) As String
        Dim str As String = String.Empty
        Using mem As New MemoryStream()
            img.Save(mem, New PngEncoder()) ' ImageSharp uses a specific encoder for PNG format
            mem.Position = 0
            Dim bBuffer As Byte() = mem.ToArray()
            str = Convert.ToBase64String(bBuffer)
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
