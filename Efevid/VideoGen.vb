Option Strict On
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Net
Imports System.Text.RegularExpressions
Imports Utils.Utils
Imports MWBot.net.WikiBot
Imports LogEngine
Imports Image = System.Drawing.Image
Imports Efevid.Ephe

Public Class VideoGen
    Property Bot As Bot
    Private Header As String = Exepath & "Res" & DirSeparator & "header.hres"
    Private Bottom As String = Exepath & "Res" & DirSeparator & "bottom.hres"
    Private Hfolder As String = Exepath & "hfiles" & DirSeparator
    Private ImgFolder As String = Exepath & "Images" & DirSeparator

    Private SettingPath As String = Exepath & "vidsettings.cfg"
    Private SettingsProvider As New Settings(SettingPath)


    Sub New(ByRef workingbot As Bot)
        Bot = workingbot
    End Sub


    Function Allefe() As Boolean
        Dim results As New List(Of Boolean)
        For i As Integer = 0 To 6
            SettingsProvider.NewVal("efecheck", False.ToString)
            SettingsProvider.NewVal("efe", "")
            Dim tdate As Date = Date.UtcNow.AddDays(-10 +i)
            Dim tdatestring As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
            EventLogger.Log("Generar efemérides " & tdatestring, "GenEfemerides")
            Dim tef As WikiBotEphe = GetEfeInfo(tdate)
            If Not tef.Revised Then
                SettingsProvider.Set("efecheck", False.ToString)
                EventLogger.Log("Efemérides no revisadas", "GenEfemerides")
                Dim efeinfopath As String = Hfolder & tdatestring & ".htm"
                IO.File.WriteAllText(efeinfopath, "Efemérides no revisadas.")
                results.Add(False)
                Continue For
            End If
            results.Add(GenEfemerides(tdate))
            SettingsProvider.Set("efecheck", True.ToString)
        Next

        Dim tresult As Boolean = True
        For Each b As Boolean In results
            tresult = (tresult And b)
        Next
        Return tresult
    End Function


    Function CheckEfe() As Boolean

        Dim tdate As Date = Date.Now.AddDays(-2)
        Dim yfile1 As String = Exepath & "hfiles" & DirSeparator & tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00") & ".htm"
        Dim yfile2 As String = Exepath & "hfiles" & DirSeparator & tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00") & ".mp4"
        Dim yfile3 As String = Exepath & "hfiles" & DirSeparator & tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00") & ".mp3"
        Dim yfile4 As String = Exepath & "hfiles" & DirSeparator & tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00") & ".txt"
        If IO.File.Exists(yfile1) Then
            IO.File.Delete(yfile1)
        End If
        If IO.File.Exists(yfile2) Then
            IO.File.Delete(yfile2)
        End If
        If IO.File.Exists(yfile3) Then
            IO.File.Delete(yfile3)
        End If
        If IO.File.Exists(yfile4) Then
            IO.File.Delete(yfile4)
        End If
        Return Allefe()
    End Function

    Function Createhfiles(ByVal tdate As Date) As Boolean
        Dim tdatestring As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
        Dim Fecha As String = tdate.ToString("d 'de' MMMM", New Globalization.CultureInfo("es-ES"))
        Dim efeinfopath As String = Hfolder & tdatestring & ".htm"

        Dim MusicDescFile As String = Hfolder & tdatestring & ".txt"

        If Not IO.Directory.Exists(ImgFolder) Then
            IO.Directory.CreateDirectory(ImgFolder)
        End If

        If Not IO.Directory.Exists(Exepath & "Res") Then
            IO.Directory.CreateDirectory(Exepath & "Res")
        End If

        If Not IO.Directory.Exists(Exepath & "hfiles") Then
            IO.Directory.CreateDirectory(Exepath & "hfiles")
        End If



        If Not IO.File.Exists(Header) Then
            IO.File.Create(Header).Close()
        End If
        If Not IO.File.Exists(Bottom) Then
            IO.File.Create(Bottom).Close()
        End If

        If Not IO.File.Exists(efeinfopath) Then
            IO.File.Create(efeinfopath).Close()
        End If

        If Not IO.File.Exists(MusicDescFile) Then
            IO.File.Create(MusicDescFile).Close()
        End If

        Dim htext As String = IO.File.ReadAllText(Header)
        Dim btext As String = IO.File.ReadAllText(Bottom)


        Dim efeinfotext As String = htext & "Efemérides del " & Fecha & " en Wikipedia, la enciclopedia libre."
        efeinfotext = efeinfotext & Environment.NewLine & Environment.NewLine & "Enlaces:"
        Dim efes As WikiBotEphe = GetEfeInfo(tdate)

        For Each ef As WikiEphe In efes.EfeDetails
            efeinfotext &= Environment.NewLine & "• "
            efeinfotext &= ef.Description.Replace("'", "")


            efeinfotext = efeinfotext & ef.Page & ": "
            efeinfotext = efeinfotext & "http://es.wikipedia.org/wiki/" & UrlWebEncode(ef.Page.Replace(" "c, "_"c))
        Next
        efeinfotext = efeinfotext & Environment.NewLine & IO.File.ReadAllText(MusicDescFile, System.Text.Encoding.UTF8)
        efeinfotext = efeinfotext & btext
        IO.File.WriteAllText(efeinfopath, efeinfotext)
        Return True
    End Function


    Function GenEfemerides(ByVal tdate As Date) As Boolean
        Dim Generated As Boolean = True
        Createhfiles(tdate)
        If Not CheckResources() Then
            EventLogger.Log("Faltan recursos en /Res", "GenEfemerides")
            Return False
        End If

        EventLogger.Log("Generando imágenes para las efemérides", "GenEfemerides")
        Dim Tpath As String = Exepath & "Images" & DirSeparator
        Dim imagename As String = "efe"
        Dim current As Integer = Createintro(imagename, Tpath, tdate)

        current = CallImages(current, imagename, Tpath, tdate)
        current = Blackout(current, imagename, Tpath)
        current = MusicInfo(current, imagename, Tpath, tdate)
        current = Blackout(current, imagename, Tpath)


        EventLogger.Log(current.ToString & " Imágenes generadas.", "GenEfemerides")

        If Not EncodeVideo(Tpath, tdate) Then
            EventLogger.EX_Log("No se ha generado video", "GenEfemerides")
            Generated = False
        End If

        EventLogger.Log("Limpiando imágenes temporales", "GenEfemerides")
        GC.Collect() 'Ye I know that it's a bad practice to directly call the GC but for some reason GDI+ doesn't release the lock on the temp file unless I do this. Not my fault tho ¯\_(ツ)_/¯
        For Each f As String In IO.Directory.GetFiles(Tpath)
            For i As Integer = 1 To 3
                Try
                    IO.File.Delete(f)
                    Exit For
                Catch ex As IO.IOException
                    EventLogger.EX_Log("Error al eliminar el archivo """ & f & """", "GenEfemerides")
                    EventLogger.EX_Log("Intento (" & i & "/3)", "GenEfemerides")
                    System.Threading.Thread.Sleep(500)
                End Try
            Next

        Next
        EventLogger.Log("Proceso completo", "GenEfemerides")
        SettingsProvider.Set("efe", tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00"))
        Return Generated
    End Function


    Function EncodeVideo(ByVal tpath As String, tdate As Date) As Boolean
        EventLogger.Log("Generando video", "EncodeVideo")
        Dim tdatestring As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
        Dim MusicFile As String = Hfolder & tdatestring & ".mp3"
        Try
            If OS.ToLower.Contains("windows") Then
                EventLogger.Log("Plataforma: Windows", "EncodeVideo")
                EventLogger.Log("Llamando a ffmpeg", "EncodeVideo")
                Using exec As New Process
                    exec.StartInfo.FileName = "ffmpeg"
                    exec.StartInfo.UseShellExecute = True
                    If IO.File.Exists(MusicFile) Then
                        exec.StartInfo.Arguments = "-y -r 29 -i """ & tpath & "efe%04d.jpg""" & " -i """ & MusicFile & """ -vcodec libx264 -preset slower -crf 19 -shortest -strict -2 """ & Hfolder & tdatestring & ".mp4"""
                    Else
                        exec.StartInfo.Arguments = "-y -r 29 -i """ & tpath & "efe%04d.jpg""" & " -vcodec libx264 -preset slower -crf 19 -shortest -strict -2 """ & Hfolder & tdatestring & ".mp4"""
                    End If
                    exec.Start()
                    exec.WaitForExit()
                End Using
            Else
                'Assume linux
                EventLogger.Log("Plataforma: Linux", "EncodeVideo")
                EventLogger.Log("Llamando a ffmpeg", "EncodeVideo")
                Using exec As New Process
                    exec.StartInfo.FileName = "ffmpeg"
                    exec.StartInfo.UseShellExecute = True
                    If IO.File.Exists(MusicFile) Then
                        exec.StartInfo.Arguments = "-y -r 29 -i """ & tpath & "efe%04d.jpg""" & " -i """ & MusicFile & """ -vcodec libx264 -preset slower -crf 19 -shortest -strict -2 """ & Hfolder & tdatestring & ".mp4"""
                    Else
                        exec.StartInfo.Arguments = "-y -r 29 -i """ & tpath & "efe%04d.jpg""" & " -vcodec libx264 -preset slower -crf 19 -shortest -strict -2 """ & Hfolder & tdatestring & ".mp4"""
                    End If
                    exec.Start()
                    exec.WaitForExit()
                End Using
            End If
        Catch ex As SystemException
            EventLogger.EX_Log("EX Encoding: " & ex.Message, "EncodeVideo")
            Return False
        End Try
        Return True
    End Function

    Function CheckResources() As Boolean
        Dim bmargin As String = ResourcesDir & "bmargin.png"
        Dim Efetxt As String = ResourcesDir & "efetxt.png"
        Dim wlogo As String = ResourcesDir & "wikipedia_logo.png"

        If Not IO.File.Exists(bmargin) Then Return False
        If Not IO.File.Exists(Efetxt) Then Return False
        If Not IO.File.Exists(tbg) Then Return False
        If Not IO.File.Exists(wlogo) Then Return False
        Return True
    End Function


    Function Createintro(ByVal imagename As String, path As String, tdate As Date) As Integer
        Dim current As Integer = 0
        Using Efeimg As Drawing.Image = Drawing.Image.FromFile(Exepath & "Res" & DirSeparator & "efetxt.png")
            Dim wikiimg As Drawing.Image = Drawing.Image.FromFile(Exepath & "Res" & DirSeparator & "wlogo.png")
            wikiimg = New Bitmap(wikiimg, New Size(CInt(wikiimg.Width / 3), CInt(wikiimg.Height / 3)))
            Using Bgimg As Drawing.Image = New Bitmap(700, 720)
                For i As Integer = 1 To 3
                    Try
                        Using tdrawing As Graphics = Graphics.FromImage(Bgimg)
                            tdrawing.Clear(Color.White)
                            tdrawing.Save()
                            Dim Fecha As String = tdate.ToString("d 'de' MMMM", New Globalization.CultureInfo("es-ES"))
                            Using fechaimg As Drawing.Image = DrawText(Fecha, New Font(FontFamily.GenericSansSerif, 35.0!, FontStyle.Regular), Color.Black, True)
                                current = DragRightToLeft(Bgimg, Efeimg, New Point(0, 0), 0.2F, imagename, path, 0)
                                Dim lastimg As Drawing.Image = Drawing.Image.FromFile(path & imagename & current.ToString("0000") & ".jpg")
                                current = DragRightToLeft(lastimg, fechaimg, New Point(lastimg.Width - fechaimg.Width, 0), 0.08F, imagename, path, current)
                                lastimg = Drawing.Image.FromFile(path & imagename & current.ToString("0000") & ".jpg")
                                Using timage As Drawing.Image = PasteImage(lastimg, wikiimg, New Point(CInt((lastimg.Width - wikiimg.Width) / 2), 150))
                                    current = PasteFadeIn(lastimg, timage, New Point(0, 0), imagename, path, current)
                                    lastimg = Drawing.Image.FromFile(path & imagename & current.ToString("0000") & ".jpg")
                                    current = Repeatimage(path, imagename, current, lastimg, 30)
                                End Using
                                lastimg.Dispose()
                            End Using
                        End Using
                        Exit For
                    Catch ex As Exception
                        EventLogger.EX_Log(ex.Message, "CreateIntro")
                        EventLogger.EX_Log("Intento (" & i & "/3)", "CreateIntro")
                        Threading.Thread.Sleep(500)
                    End Try
                Next
            End Using
            wikiimg.Dispose()
        End Using
        Return current
    End Function


    Function Blackout(ByVal current As Integer, imagename As String, path As String) As Integer
        Dim lastimg As Drawing.Image = Drawing.Image.FromFile(path & imagename & current.ToString("0000") & ".jpg")
        Dim outimg As Drawing.Image = New Bitmap(700, 720)
        Using timg As Drawing.Image = New Bitmap(700, 720)
            For i As Integer = 1 To 3
                Try
                    Using gr As Graphics = Graphics.FromImage(timg)
                        gr.Clear(Color.Black)
                        gr.Save()
                        outimg = CType(timg.Clone, Drawing.Image)
                    End Using
                    Exit For
                Catch ex As Exception
                    EventLogger.EX_Log(ex.Message, "Blackout")
                    EventLogger.EX_Log("Intento (" & i & "/3)", "Blackout")
                    Threading.Thread.Sleep(500)
                End Try
            Next
        End Using
        current = PasteFadeIn(lastimg, outimg, New Point(0, 0), imagename, path, current)
        outimg.Dispose()
        Return current
    End Function

    Function GetEfeInfo(ByVal tdate As Date) As WikiBotEphe
        Dim efelist As New List(Of Tuple(Of String, String()))
        Dim efetxt As String() = GetEfetxt(tdate)
        Dim Varlist As New Dictionary(Of String, List(Of Tuple(Of String, String)))

        If efetxt.Count > 0 Then
            For Each s As String In efetxt
                If Not String.IsNullOrWhiteSpace(s) Then
                    Dim var As String = s.Split("="c)(0).Trim
                    Dim vValue As String = s.Split("="c)(1).Trim
                    Dim vProperty As String = String.Empty
                    If var.Contains("."c) Then
                        vProperty = var.Split("."c)(1).Trim
                        var = var.Split("."c)(0).Trim
                    End If
                    If Not Varlist.Keys.Contains(var) Then
                        Varlist.Add(var, New List(Of Tuple(Of String, String)))
                    End If
                    If Not String.IsNullOrWhiteSpace(vProperty) Then
                        Varlist(var).Add(New Tuple(Of String, String)(vProperty, vValue))
                    Else
                        Varlist(var).Add(New Tuple(Of String, String)("", vValue))
                    End If
                End If
            Next
        End If

        Dim Efinfo As New WikiBotEphe
        For Each k As String In Varlist.Keys
            If k = "revisadas" Then
                Dim varrev As String = Varlist("revisadas")(0).Item2
                If varrev = "sí" Then
                    Efinfo.Revised = True
                Else
                    Efinfo.Revised = False
                End If
            End If
            If k = "página para imagen" Then
                Dim varval As String = Varlist("página para imagen")(0).Item2
                Efinfo.ImagePage = varval
            End If
            If k = "wikitexto" Then
                Dim varval As String = Varlist("wikitexto")(0).Item2
                Efinfo.Wikitext = varval
            End If
            If k.Contains("-"c) Then
                Dim tef As New WikiEphe
                For Each t As Tuple(Of String, String) In Varlist(k)
                    Dim varname As String = t.Item1
                    Dim varval As String = t.Item2
                    If varname = "año" Then
                        tef.Year = Integer.Parse(varval)
                    End If
                    If varname = "descripción" Then
                        tef.Description = varval
                    End If
                    If varname = "imagen" Then
                        tef.Image = varval
                    End If
                    If varname = "página" Then
                        tef.Page = varval
                    End If
                    If varname = "tamaño" Then
                        tef.TextSize = Double.Parse(varval.Replace(","c, DecimalSeparator))
                    End If
                    If varname = "tipo" Then
                        Select Case varval
                            Case "F"
                                tef.Type = WikiEpheType.Defunción
                            Case "A"
                                tef.Type = WikiEpheType.Acontecimiento
                            Case "N"
                                tef.Type = WikiEpheType.Nacimiento
                        End Select
                    End If
                Next
                Efinfo.EfeDetails.Add(tef)
            End If
        Next
        Return Efinfo
    End Function

    Function GetEfetxt(ByVal tdate As Date) As String()
        Dim fechastr As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
        Dim efetxt As Uri = New Uri(CType(SettingsProvider.Get("JBURI"), String) & fechastr & "/" & fechastr & ".txt")
        Dim txt As String = Bot.GET(efetxt)
        If String.IsNullOrWhiteSpace(txt) Then Return {""}
        Dim txtlist As List(Of String) = txt.Split(CType(vbLf, Char())).ToList
        For i As Integer = 0 To txtlist.Count - 1
            txtlist(i) = txtlist(i).Replace("█", Environment.NewLine)
        Next
        Return txtlist.ToArray
    End Function


    Function CallImages(ByVal current As Integer, imagename As String, path As String, tDate As Date) As Integer
        Dim efes As WikiBotEphe = GetEfeInfo(tDate)
        Dim c As Integer = 0
        For Each ef As WikiEphe In efes.EfeDetails
            Dim año As Integer = ef.Year
            Dim Description As String = ef.Description
            Dim Commonsimg As String = "File:" & ef.Image
            Dim commonsimgdata As Tuple(Of Drawing.Image, String()) = GetCommonsFile(Commonsimg)
            Dim licence As String = commonsimgdata.Item2(0)
            Dim licenceurl As String = commonsimgdata.Item2(1)
            Dim author As String = commonsimgdata.Item2(2)
            Using cimage As Drawing.Image = commonsimgdata.Item1
                current = Createimages(path, imagename, current, ef.Image, cimage, licence, licenceurl, author, año, Description, ef.TextSize)
            End Using
            c += 6
        Next
        Return current
    End Function

    Function Createimages(ByVal Path As String, imagename As String, current As Integer, efimgname As String, efimg As Drawing.Image, licencename As String, licenceurl As String, artist As String, year As Integer, description As String, textsize As Double) As Integer
        If licencename.ToLower = "public domain" Then licencename = "En dominio público"
        If Not String.IsNullOrWhiteSpace(licenceurl) Then licencename = licencename & " (" & licenceurl & ")"
        Dim CommonsName As String = "Imagen en Wikimedia Commons: " & NormalizeUnicodetext(efimgname)
        Dim detailstext As String = CommonsName & Environment.NewLine & "Autor: " & artist & Environment.NewLine & "Licencia: " & licencename
        Dim yeardiff As Integer = Date.Now.Year - year
        Dim syeardiff As String = "Hace " & yeardiff.ToString & " años"
        Dim hratio As Double = efimg.Width / efimg.Height

        efimg = New Bitmap(efimg, New Size(700, CInt(720 / hratio)))
        If efimg.Height < 720 Then
            For i As Integer = 1 To 3
                Try
                    Using timg As Drawing.Image = New Bitmap(700, 720)
                        Using gr As Graphics = Graphics.FromImage(timg)
                            gr.Clear(Color.White)
                            gr.Save()
                            efimg = PasteImage(timg, efimg, New Point(0, CInt((720 - efimg.Height) / 2)))
                        End Using
                    End Using
                    Exit For
                Catch ex As Exception
                    EventLogger.EX_Log(ex.Message, "Createimages")
                    EventLogger.EX_Log("Intento (" & i & "/3)", "Createimages")
                    Threading.Thread.Sleep(500)
                End Try
            Next
        End If

        Dim lastimg As Drawing.Image = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
        lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
        current = PasteFadeIn(lastimg, efimg, New Point(0, 0), imagename, Path, current)
        lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")

        Using yrimg As Drawing.Image = DrawText2(year.ToString, New Font(FontFamily.GenericSansSerif, 70.0!, FontStyle.Regular), Color.Black, Color.White, True)

            Using añoimg As Drawing.Image = PasteImage(lastimg, yrimg, New Point(0, 0))
                current = PasteFadeIn(lastimg, añoimg, New Point(0, 0), imagename, Path, current)
                lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
            End Using

            Using dimg As Drawing.Image = DrawText2(syeardiff, New Font(FontFamily.GenericSansSerif, 30.0!, FontStyle.Regular), Color.Black, Color.White, True)
                Using Diffimg As Drawing.Image = PasteImage(lastimg, dimg, New Point(0, 90))
                    current = PasteFadeIn(lastimg, Diffimg, New Point(0, 0), imagename, Path, current)
                    lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
                End Using
                Using bgt As Drawing.Image = Drawing.Image.FromFile(Exepath & "Res" & DirSeparator & "tbg.png")
                    lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
                    Dim defbgt As Drawing.Image = PasteImage(bgt, yrimg, New Point(0, 0)) 'bg
                    defbgt = PasteImage(defbgt, DrawText2(year.ToString, New Font(FontFamily.GenericSansSerif, 70.0!, FontStyle.Regular), Color.Black, Color.White, True), New Point(0, 0))

                    current = PasteFadeIn(lastimg, defbgt, New Point(0, 0), imagename, Path, current)
                    defbgt.Dispose()
                    lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
                End Using
            End Using
        End Using

        Using descimg As Drawing.Image = DrawText(description, New Font(FontFamily.GenericSansSerif, Convert.ToSingle(3.0! * textsize), FontStyle.Regular), Color.White, True)
            Using timage As Drawing.Image = PasteImage(lastimg, descimg, New Point(CInt((lastimg.Width - descimg.Width) / 2), 350))
                current = PasteFadeIn(lastimg, timage, New Point(0, 0), imagename, Path, current)
                lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
            End Using
        End Using

        Using bmargin As Drawing.Image = Drawing.Image.FromFile(Exepath & "Res" & DirSeparator & "bmargin.png")
            Dim bimg As Drawing.Image = PasteImage(lastimg, bmargin, New Point(0, 642))
            current = PasteFadeIn(lastimg, bimg, New Point(0, 0), imagename, Path, current)
            lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
        End Using

        Using detailsimg As Drawing.Image = DrawText(detailstext, New Font(FontFamily.GenericMonospace, 10.0!, FontStyle.Regular), Color.LightGray, False)
            current = DragRightToLeft(lastimg, detailsimg, New Point(0, 650), 0.6F, imagename, Path, current)
            lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
            current = Repeatimage(Path, imagename, current, lastimg, 45)
        End Using

        For i As Integer = 1 To 3
            Try
                Using transimg As Drawing.Image = New Bitmap(700, 720)
                    Using g As Graphics = Graphics.FromImage(transimg)
                        g.Clear(Color.White)
                        g.Save()
                    End Using
                    current = PasteFadeIn(lastimg, transimg, New Point(0, 0), imagename, Path, current)
                    lastimg.Dispose()
                End Using
                Exit For
            Catch ex As Exception
                EventLogger.EX_Log(ex.Message, "Createimages")
                EventLogger.EX_Log("Intento (" & i & "/3)", "Createimages")
                Threading.Thread.Sleep(500)
            End Try
        Next

        Return current
    End Function


    Function MusicInfo(ByVal current As Integer, imagename As String, path As String, tdate As Date) As Integer
        Dim tdatestring As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
        Dim MusicDescFile As String = Hfolder & tdatestring & ".txt"
        If Not IO.File.Exists(MusicDescFile) Then Return current

        Dim lastimg As Drawing.Image = Drawing.Image.FromFile(path & imagename & current.ToString("0000") & ".jpg")
        Dim description As String = IO.File.ReadAllText(MusicDescFile, System.Text.Encoding.UTF8)

        Using descimg As Drawing.Image = DrawText(description, New Font(FontFamily.GenericSansSerif, Convert.ToSingle(3.0! * 4.5), FontStyle.Regular), Color.White, True)
            Using timage As Drawing.Image = PasteImage(lastimg, descimg, New Point(CInt((lastimg.Width - descimg.Width) / 2), 250))
                current = PasteFadeIn(lastimg, timage, New Point(0, 0), imagename, path, current)
                current = Repeatimage(path, imagename, current, timage, 30)
            End Using
        End Using
        Return current
    End Function


    Public Function Repeatimage(ByVal Path As String, imagename As String, current As Integer, efimg As Drawing.Image, repetitions As Integer) As Integer
        For i = 0 To repetitions
            current += 1
            For a As Integer = 1 To 3
                Try
                    Using timg As Drawing.Image = CType(efimg.Clone, Drawing.Image)
                        timg.Save(Path & imagename & current.ToString("0000") & ".jpg", Imaging.ImageFormat.Jpeg)
                    End Using
                    Exit For
                Catch ex As Exception
                    EventLogger.EX_Log(ex.Message, "Repeatimage")
                    EventLogger.EX_Log("Intento (" & a & "/3)", "Repeatimage")
                    Threading.Thread.Sleep(500)
                End Try
            Next
        Next
        Return current
    End Function

    Public Function DragRightToLeft(ByVal Bgimg As Drawing.Image, ByVal fimg As Drawing.Image, imgpos As Point, ByVal speed As Double, Imagename As String, Imagepath As String, Startingindex As Integer) As Integer
        Dim last As Integer = Startingindex
        Dim imglist As New List(Of Drawing.Image)
        Using bgim As Drawing.Image = CType(Bgimg.Clone, Drawing.Image)
            Using fim As Drawing.Image = CType(fimg.Clone, Drawing.Image)
                Dim twidth As Integer = bgim.Width
                For i As Integer = twidth To imgpos.X Step -1
                    Using tfimg As Drawing.Image = CType(fim.Clone, Drawing.Image)
                        Using tBgimg As Drawing.Image = CType(bgim.Clone, Drawing.Image)
                            Dim tpos As Integer = CInt(twidth * (Math.E ^ (-(twidth - i) * speed)))
                            If (tpos < (imgpos.X + 5)) Then
                                Using nimg As Drawing.Image = PasteImage(tBgimg, tfimg, New Point(imgpos.X, imgpos.Y))
                                    imglist.Add(CType(nimg.Clone, Drawing.Image))
                                End Using
                                Exit For
                            End If
                            Using nimg As Drawing.Image = PasteImage(tBgimg, tfimg, New Point(tpos, imgpos.Y))
                                imglist.Add(CType(nimg.Clone, Drawing.Image))
                            End Using
                        End Using
                    End Using
                Next
                For i As Integer = 0 To imglist.Count - 1
                    For a As Integer = 1 To 3
                        Try
                            Using timg As Drawing.Image = CType(imglist(i).Clone, Drawing.Image)
                                timg.Save(Imagepath.ToString & DirSeparator & Imagename & (last + i + 1).ToString("0000") & ".jpg", Imaging.ImageFormat.Jpeg)
                            End Using
                            Exit For
                        Catch ex As Exception
                            EventLogger.EX_Log(ex.Message, "DragRightToLeft")
                            EventLogger.EX_Log("Intento (" & a & "/3)", "DragRightToLeft")
                            Threading.Thread.Sleep(500)
                        End Try
                    Next
                Next
            End Using
        End Using
        Dim imgcount As Integer = imglist.Count
        imglist = Nothing
        last = last + imgcount
        Return last
    End Function

    Public Function PasteImage(ByVal bgimage As Drawing.Image, ByVal frontimage As Drawing.Image, ByVal startpos As Point) As Drawing.Image
        Dim newimg As Bitmap = CType(bgimage.Clone, Bitmap)

        For i As Integer = 1 To 3
            Try
                Using fimg As Bitmap = CType(frontimage.Clone, Bitmap)
                    Using g As Graphics = Graphics.FromImage(newimg)
                        g.DrawImage(fimg, New Point(startpos.X, startpos.Y))
                        g.Save()
                    End Using
                End Using
                Exit For
            Catch ex As Exception
                EventLogger.EX_Log(ex.Message, "PasteImage")
                EventLogger.EX_Log("Intento (" & i & "/3)", "PasteImage")
                Threading.Thread.Sleep(500)
            End Try
        Next
        Dim timg As Drawing.Image = newimg
        Return timg
    End Function

    Public Function PasteFadeIn(ByVal Bgimg As Drawing.Image, ByVal fimg As Drawing.Image, imgpos As Point, Imagename As String, Imagepath As String, Startingindex As Integer) As Integer
        Dim Counter As Integer = Startingindex
        Using nimg As Drawing.Image = PasteImage(CType(Bgimg.Clone, Drawing.Image), CType(fimg.Clone, Drawing.Image), imgpos)
            Counter = Fadein(Imagepath, Imagename, Startingindex, CType(Bgimg.Clone, Drawing.Image), CType(nimg.Clone, Drawing.Image))
        End Using
        Return Counter
    End Function

    ''' <summary>
    ''' La imagen de fondo debe tener el mismo tamaño que la de frente
    ''' </summary>
    ''' <param name="Bgimg"></param>
    ''' <param name="image"></param>
    ''' <returns></returns>
    Public Function Fadein(ByVal Path As String, imagename As String, current As Integer, ByVal Bgimg As Drawing.Image, ByVal image As Drawing.Image) As Integer

        For i As Integer = 1 To 3
            Try
                Using orig As Bitmap = CType(image.Clone, Bitmap)
                    Using bg As Bitmap = CType(Bgimg.Clone, Bitmap)
                        For b As Integer = 0 To 30
                            Using graphic As Graphics = Graphics.FromImage(bg)
                                Dim tmatrix As Single()() = {
                    New Single() {1, 0, 0, 0, 0},
                    New Single() {0, 1, 0, 0, 0},
                    New Single() {0, 0, 1, 0, 0},
                    New Single() {0, 0, 0, Convert.ToSingle((1 / 30) * b), 0},
                    New Single() {0, 0, 0, 0, 1}
                    }
                                Dim cmatrix As Imaging.ColorMatrix = New Imaging.ColorMatrix(tmatrix)
                                Dim imageatt As New Imaging.ImageAttributes()
                                imageatt.SetColorMatrix(cmatrix, Imaging.ColorMatrixFlag.Default, Imaging.ColorAdjustType.Bitmap)
                                Dim trectangle As New Rectangle(0, 0, bg.Width, bg.Height)
                                graphic.DrawImage(orig, trectangle, 0F, 0F, bg.Width, bg.Height, GraphicsUnit.Pixel, imageatt)
                                graphic.Save()
                            End Using
                            bg.Save(Path & imagename & (current + 1).ToString("0000") & ".jpg", Imaging.ImageFormat.Jpeg)
                            current = current + 1
                        Next
                        orig.Save(Path & imagename & (current + 1).ToString("0000") & ".jpg", Imaging.ImageFormat.Jpeg)
                        current = current + 1
                    End Using
                End Using
                Exit For
            Catch ex As Exception
                EventLogger.EX_Log(ex.Message, "Fadein")
                EventLogger.EX_Log("Intento (" & i & "/3)", "Fadein")
                Threading.Thread.Sleep(500)
            End Try
        Next
        Return current
    End Function

    Function Ignore(ByVal callbackdata As IntPtr) As Boolean
        Return False
    End Function

    Public Function DrawText2(ByVal text As String, ByVal font As Font, ByVal textcolor As Color, ByVal backcolor As Color, ByVal center As Boolean) As Drawing.Image
        text = text.Replace("'''", "") 'Por ahora ignoremos las negritas
        Dim Lines As String() = GetLines(text)
        Dim images As New List(Of Drawing.Image)
        For Each line As String In Lines
            images.Add(DrawSpecialText(line, font, textcolor, backcolor))
        Next

        Dim timg As Drawing.Image = New Bitmap(1, 1)
        Dim totalwidth As Integer = 0
        Dim totalheight As Integer = 0
        For Each limage As Drawing.Image In images
            If limage.Width > totalwidth Then
                totalwidth = limage.Width
            End If
            totalheight = totalheight + limage.Height
        Next
        timg = New Bitmap(totalwidth, totalheight)
        Dim lastheight As Integer = 0
        For Each limage As Drawing.Image In images
            If center Then
                timg = PasteImage(timg, limage, New Point(CInt((totalwidth - limage.Width) / 2), lastheight))
                lastheight = lastheight + limage.Height
            Else
                timg = PasteImage(timg, limage, New Point(0, lastheight))
                lastheight = lastheight + limage.Height
            End If
        Next
        Return timg
    End Function


    Public Function DrawSpecialText(ByVal text As String, ByVal font As Font, ByVal textColor As Color, ByVal backColor As Color) As Drawing.Image
        text = text.Replace("'''", "")
        text = text.Replace(Environment.NewLine, "").Replace(vbLf, "").Replace(vbCr, "").Replace(vbCrLf, "")
        Dim img As Drawing.Image = New Bitmap(1, 1)

        Dim OriginalDrawing As Graphics = Graphics.FromImage(img)
        If String.IsNullOrEmpty(text) Then Return img

        Dim boldfont As New Font(font.FontFamily, font.Size, FontStyle.Regular)
        Dim textSize As SizeF = OriginalDrawing.MeasureString(text, boldfont)
        img = New Bitmap(CInt(Math.Ceiling(textSize.Width) * 1.2F), CInt((Math.Ceiling(textSize.Height)) * 1.1F))

        For i As Integer = 1 To 3
            Try
                Using drawing As Graphics = Graphics.FromImage(img)
                    drawing.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias
                    drawing.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

                    Using outlinePath As New Drawing2D.GraphicsPath
                        outlinePath.AddString(text, boldfont.FontFamily, FontStyle.Regular, boldfont.SizeInPoints, New Point(0, 0), StringFormat.GenericTypographic)
                        drawing.FillPath(Brushes.LightGray, outlinePath)
                        drawing.DrawPath(New Pen(textColor), outlinePath)
                    End Using
                    drawing.Save()
                End Using
                Exit For
            Catch ex As Exception
                EventLogger.EX_Log(ex.Message, "DrawSpecialText")
                EventLogger.EX_Log("Intento (" & i & "/3)", "DrawSpecialText")
                Threading.Thread.Sleep(500)
            End Try
        Next

        Return img
    End Function

    Public Function DrawText(ByVal text As String, ByVal font As Font, ByVal textColor As Color, ByVal center As Boolean) As Drawing.Image
        Dim Lines As String() = GetLines(text)
        Dim images As New List(Of Drawing.Image)

        For Each line As String In Lines
            images.Add(DrawLine(line, font, textColor))
        Next

        Dim timg As Drawing.Image = New Bitmap(1, 1)
        Dim totalwidth As Integer = 0
        Dim totalheight As Integer = 0

        For Each limage As Drawing.Image In images
            If limage.Width > totalwidth Then
                totalwidth = limage.Width
            End If
            totalheight = totalheight + limage.Height
        Next
        timg = New Bitmap(totalwidth, totalheight)

        Dim lastheight As Integer = 0
        For Each limage As Drawing.Image In images
            If center Then
                If limage.Width < timg.Width Then
                    timg = PasteImage(timg, limage, New Point(CInt((timg.Width - limage.Width) / 2), lastheight))
                Else
                    timg = PasteImage(timg, limage, New Point(0, lastheight))
                End If
                lastheight = lastheight + limage.Height
            Else
                timg = PasteImage(timg, limage, New Point(0, lastheight))
                lastheight = lastheight + limage.Height
            End If
        Next
        Return timg
    End Function

    Public Function DrawLine(ByVal text As String, ByVal font As Font, ByVal textColor As Color) As Drawing.Image
        Dim img As Drawing.Image = New Bitmap(1, 1)
        text = text.Replace("'''", "") 'Ignoremos negritas
        text = text.Replace(Environment.NewLine, "").Replace(vbLf, "").Replace(vbCr, "").Replace(vbCrLf, "") 'No saltos de linea

        Dim sf As New StringFormat With {
            .Alignment = StringAlignment.Near,
            .FormatFlags = StringFormatFlags.NoClip,
            .Trimming = StringTrimming.None,
            .HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Hide
             }

        Using drawing As Graphics = Graphics.FromImage(img)
            If String.IsNullOrEmpty(text) Then Return img
            Dim textSize As SizeF = drawing.MeasureString(text, font)
            img = New Bitmap(CType(textSize.Width, Integer), CType(textSize.Height, Integer))
        End Using


        For i As Integer = 1 To 3
            Try
                Using drawing As Graphics = Graphics.FromImage(img)
                    Using textBrush As Brush = New SolidBrush(textColor)
                        Dim tstring As String = text.Trim
                        drawing.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                        drawing.DrawString(tstring, font, textBrush, 0, 0, sf)
                        drawing.Save()
                    End Using
                End Using
                Exit For
            Catch ex As Exception
                EventLogger.EX_Log(ex.Message, "DrawLine")
                EventLogger.EX_Log("Intento (" & i & "/3)", "DrawLine")
                Threading.Thread.Sleep(500)
            End Try
        Next

        Return img
    End Function

    Function GetCommonsFile(ByVal CommonsFilename As String) As Tuple(Of Drawing.Image, String())
        Dim responsestring As String = NormalizeUnicodetext(Bot.GETQUERY("action=query&format=json&titles=" & UrlWebEncode(CommonsFilename) & "&prop=imageinfo&iiprop=extmetadata|url&iiurlwidth=500"))
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
        Dim img As Drawing.Image = New Bitmap(1, 1)
        If thumburlmatches.Count > 0 Then
            img = PicFromUrl(thumburlmatches(0))
        End If
        If String.IsNullOrWhiteSpace(author) Or (author.ToLower.Contains("unknown")) Then
            author = "Desconocido"
        End If
        Return New Tuple(Of Drawing.Image, String())(img, {licence, licenceurl, author})
    End Function

    Function PicFromUrl(ByVal url As String) As Drawing.Image
        Dim img As Drawing.Image = New Bitmap(1, 1)
        Try
            Dim request = WebRequest.Create(url)
            Using response = request.GetResponse()
                Using stream = response.GetResponseStream()
                    img = CType(Drawing.Image.FromStream(stream).Clone, Drawing.Image)
                End Using
            End Using
            Return img
        Catch ex As Exception
            img.Dispose()
            Return Nothing
        End Try
    End Function
End Class