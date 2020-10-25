Option Strict On
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports Efevid.Ephe
Imports MWBot.net.WikiBot
Imports Util = Utils.Utils

Public Class EpheProvider

    Private WorkerBot As Bot
    Private useAlt As Boolean = False

    Public Sub New(worker As Bot)
        WorkerBot = worker
        Dim alt As Integer = CType(SettingsProvider.Get("USEALTSOURCE"), Integer)
        If alt = 0 Then
            useAlt = False
        Else
            useAlt = True
        End If
    End Sub

    Public Shared Function GetDateAsSpaString(ByVal tDate As Date) As String
        Dim esES As New CultureInfo("es-ES")
        Return tDate.ToString("d ""de"" MMMM", esES)
    End Function

    Public Function GetEphes(ByVal tDate As Date) As WikiBotEphe
        Dim ephes As New List(Of WikiEphe)
        Dim tDateAsString As String = GetDateAsSpaString(tDate)

        Dim jembotEphe As WikiBotEphe = GetEfeInfoFromjembot(tDate)

        If Not jembotEphe.Revised And useAlt Then
            Dim epheTemplateContent As String = WorkerBot.Getpage("Plantilla:Efemérides_-_" & tDateAsString).Content()
            Dim events As String() = GetEvents(epheTemplateContent)
            For Each epheEvent As String In events
                Dim eph As New WikiEphe With {
                    .Description = GetEpheDescriptionFromEventString(epheEvent),
                    .Page = GetEphePageFromEventString(epheEvent),
                    .Image = GetEpheImageFromPage(.Page),
                    .TextSize = 10D,
                    .Type = GetEpheTypeFromEventString(epheEvent),
                    .Year = GetEpheYearFromEventString(epheEvent)
                    }

                If String.IsNullOrWhiteSpace(eph.Image) Then
                    Select Case eph.Type
                        Case WikiEpheType.Acontecimiento
                            eph.Image = "Social_Content_Curation.svg"
                        Case WikiEpheType.Defunción
                            eph.Image = "Clipart_of_a_Tealight.svg"
                        Case WikiEpheType.Nacimiento
                            eph.Image = "Empty_Star.svg"
                    End Select
                End If
                ephes.Add(eph)
            Next
            Dim wEphes As New WikiBotEphe With {
                .EfeDetails = ephes,
                .Revised = True,
                .Wikitext = epheTemplateContent}
            Return wEphes
        End If
        Return jembotEphe
    End Function


    Private Function GetEvents(ByVal pageText As String) As String()
        Dim eventList As New List(Of String)
        For Each m As Match In Regex.Matches(pageText, "\* (\[\[.+)")
            eventList.Add(m.Groups(1).Value)
        Next
        Return eventList.ToArray
    End Function


    Private Function GetEpheYearFromEventString(ByVal eventStr As String) As Integer
        Dim introStr As String = eventStr.Substring(0, 15)
        Dim yearStr As String() = Util.TextInBetween(introStr, "[[", "]]")
        Dim year As Integer = 0
        For Each y As String In yearStr
            year = Integer.Parse(y)
        Next
        Return year
    End Function

    Private Function GetEpheTypeFromEventString(ByVal eventStr As String) As WikiEpheType
        Dim eventLenght As Integer = eventStr.Length
        If eventLenght > 80 Then eventLenght = 80
        Dim introStr As String = eventStr.Substring(0, eventLenght)

        Dim nac As Integer = Util.CountOccurrences(introStr.ToLowerInvariant, " nace ")
        Dim fall As Integer = Util.CountOccurrences(introStr.ToLowerInvariant, " fallece ")
        fall += Util.CountOccurrences(introStr, " muere ")
        fall += Util.CountOccurrences(introStr, " asesinado ")

        Dim eType As WikiEpheType

        If nac > 0 Then
            eType = WikiEpheType.Nacimiento
        End If

        If fall > 0 Then
            eType = WikiEpheType.Defunción
        End If

        If (nac + fall = 0) Then
            eType = WikiEpheType.Acontecimiento
        End If

        Return eType
    End Function

    Private Function GetEphePageFromEventString(ByVal eventStr As String) As String
        Dim links As String() = Util.TextInBetweenInclusive(eventStr, "[[", "]]")
        Dim tPage As String = Util.GetLinkText(links(1)).Item2
        Return tPage
    End Function

    Private Function GetEpheImageFromPage(ByVal pageName As String) As String
        Dim tImg As String = WorkerBot.GetImageExtract(pageName)
        Return tImg
    End Function

    Private Function GetEpheDescriptionFromEventString(ByVal eventStr As String) As String
        Dim tmatches As MatchCollection = Regex.Matches(eventStr, "([—-]) (.+?)(\.|$)")
        Dim desc As String = String.Empty
        For Each m As Match In tmatches
            desc = m.Groups(2).Value
            Exit For
        Next
        desc = desc.Trim().TrimEnd("."c).Trim() & "."
        desc = WorkerBot.GetFlattenText(desc)
        desc = Regex.Replace(desc, "( \(.+?en la imag.+?\))", "")

        Return desc
    End Function


    Function GetEfetxtFromJembot(ByVal tdate As Date) As String()
        Dim fechastr As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
        Dim efetxt As Uri = New Uri(CType(SettingsProvider.Get("JBURI"), String) & fechastr & "/" & fechastr & ".txt")
        Dim txt As String = String.Empty
        txt = WorkerBot.GET(efetxt)
        If String.IsNullOrWhiteSpace(txt) Then Return {""}
        Dim txtlist As List(Of String) = txt.Split(CType(vbLf, Char())).ToList
        For i As Integer = 0 To txtlist.Count - 1
            txtlist(i) = txtlist(i).Replace("█", Environment.NewLine)
        Next
        Return txtlist.ToArray
    End Function

    Function GetEfeInfoFromjembot(ByVal tdate As Date) As WikiBotEphe
        Dim efelist As New List(Of Tuple(Of String, String()))
        Dim efetxt As String() = GetEfetxtFromJembot(tdate)
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
                        tef.Description = varval.Replace("'''", "")
                    End If
                    If varname = "imagen" Then
                        tef.Image = varval
                    End If
                    If varname = "página" Then
                        tef.Page = varval
                    End If
                    If varname = "tamaño" Then
                        tef.TextSize = Double.Parse(varval.Replace(","c, Util.DecimalSeparator))
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

End Class
