Option Strict On
Imports Efevid.Ephe
Imports MWBot.net.WikiBot
Imports MWBot.net.Utility
Imports MWBot.net.Utility.Utils

Module Main
    Public WorkerDir As String
    Public TempDir As String
    Public ResourcesDir As String
    Public ResultDir As String


    Public statuspath As String
    Public triggerFile As String


    Public ReadOnly ConfigFilePath As String = Exepath & "Config.cfg"
    Public ReadOnly Logpath As String = Exepath & "VidLog.psv"
    Public ReadOnly UserPath As String = Exepath & "Users.psv"

    Public ReadOnly EventLogger As New SimpleLogger(Logpath, UserPath, "Efevid", True)
    Public ReadOnly Log_Filepath As String = Exepath & "VidLog.psv"
    Public ReadOnly User_filepath As String = Exepath & "Users.psv"
    Public ReadOnly User As String = "Efevid"

    Public SettingsPath As String
    Public SettingsProvider As Settings


    Sub Main()
        LoadSettings()
        Do While True
            If UpdateRequested() Then
                Dim workerBot As Bot = New Bot(ConfigFilePath, EventLogger)
                Dim EphProv As New EpheProvider(workerBot)

                For i As Integer = 0 To 7
                    Dim tdate As Date = Date.Now.AddDays(-1 + i)
                    Dim reqEphes As WikiEphe() = EphProv.GetEphes(tdate).EfeDetails.ToArray()
                    Dim revised As Boolean = EphProv.GetEphes(tdate).Revised
                    If revised Then
                        Dim tdatestring As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
                        Try
                            If Not IO.File.Exists(ResultDir & tdatestring & ".txt") Then
                                IO.File.Create(ResultDir & tdatestring & ".txt").Close()
                            End If
                            Dim outrotxt As String = IO.File.ReadAllText(ResultDir & tdatestring & ".txt")
                            Dim gen As New NewVideoGen(workerBot, {ResourcesDir & "intro.svg", ResourcesDir & "eph.svg", ResourcesDir & "outro.svg"}, TempDir, tdate, reqEphes, outrotxt)
                            gen.Generate()
                        Catch ex As Exception
                            EventLogger.EX_Log(ex.Message, User)
                            EventLogger.EX_Log(ex.Source, User)
                            EventLogger.EX_Log(ex.StackTrace, User)
                        Finally
                            IO.File.Delete(triggerFile)
                        End Try
                    Else
                        EventLogger.Log("Efemérides " & tdate.ToString("dd/MM/yy") & " no revisadas", "Main")
                    End If
                Next
                EventLogger.Log("Proceso completo", "Main")
            End If
            IO.File.Delete(triggerFile)
            System.Threading.Thread.Sleep(500)
        Loop

    End Sub

    Function UpdateRequested() As Boolean
        If Not IO.Directory.Exists(WorkerDir) Then
            IO.Directory.CreateDirectory(WorkerDir)
        End If
        If Not IO.Directory.Exists(TempDir) Then
            IO.Directory.CreateDirectory(TempDir)
        End If
        If Not IO.Directory.Exists(ResourcesDir) Then
            IO.Directory.CreateDirectory(ResourcesDir)
        End If
        If Not IO.Directory.Exists(ResultDir) Then
            IO.Directory.CreateDirectory(ResultDir)
        End If
        If Not IO.File.Exists(statuspath) Then
            IO.File.Create(statuspath).Close()
        End If
        Return IO.File.Exists(triggerFile)
    End Function

    Sub LoadSettings()
        SettingsPath = Exepath & "Settings.psv"
        SettingsProvider = New Settings(SettingsPath)

        Dim jburi As Boolean = SettingsProvider.Contains("JBURI")
        If Not jburi Then
            SettingsProvider.NewVal("JBURI", "https://jembot.toolforge.org/ef/gen/")
        End If

        Dim useAlternativeSource As Boolean = SettingsProvider.Contains("USEALTSOURCE")
        If Not useAlternativeSource Then
            SettingsProvider.NewVal("USEALTSOURCE", 1)
        End If

        Dim wdir As Boolean = SettingsProvider.Contains("WORKER_DIR")
        If Not wdir Then
            SettingsProvider.NewVal("WORKER_DIR", Exepath & "worker" & DirSeparator)
        End If
        WorkerDir = CType(SettingsProvider.Get("WORKER_DIR"), String)

        Dim tmpdir As Boolean = SettingsProvider.Contains("TEMP_DIR")
        If Not tmpdir Then
            SettingsProvider.NewVal("TEMP_DIR", Exepath & "temp" & DirSeparator)
        End If
        TempDir = CType(SettingsProvider.Get("TEMP_DIR"), String)

        Dim resdir As Boolean = SettingsProvider.Contains("RES_DIR")
        If Not resdir Then
            SettingsProvider.NewVal("RES_DIR", Exepath & "resources" & DirSeparator)
        End If
        ResourcesDir = CType(SettingsProvider.Get("RES_DIR"), String)

        Dim resultd As Boolean = SettingsProvider.Contains("RESULT_DIR")
        If Not resultd Then
            SettingsProvider.NewVal("RESULT_DIR", Exepath & "result" & DirSeparator)
        End If
        ResultDir = CType(SettingsProvider.Get("RESULT_DIR"), String)

        Dim logo As Boolean = SettingsProvider.Contains("INTRO_LOGO")
        If Not logo Then
            SettingsProvider.NewVal("INTRO_LOGO", ResourcesDir & "wlogo.png")
        End If

        Dim vidName As Boolean = SettingsProvider.Contains("VID_NAME")
        If Not vidName Then
            SettingsProvider.NewVal("VID_NAME", "Efemérides")
        End If


        statuspath = WorkerDir & "status.htm"
        triggerFile = WorkerDir & "efevid.runme"

    End Sub




End Module
