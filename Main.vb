Imports MWBot.net
Imports MWBot.net.WikiBot
Imports Utils.Utils

Module Main
    Public ReadOnly Header As String = Exepath & "Res" & DirSeparator & "header.hres"
    Public ReadOnly Bottom As String = Exepath & "Res" & DirSeparator & "bottom.hres"
    Public ReadOnly Hfolder As String = Exepath & "hfiles" & DirSeparator
    Public ReadOnly ImgFolder As String = Exepath & "Images" & DirSeparator
    Public ReadOnly Reqfolder As String = Exepath & "req" & DirSeparator
    Public ReadOnly ConfigFilePath As String = Exepath & "Config.cfg"
    Public ReadOnly Logpath As String = Exepath & "VidLog.psv"
    Public ReadOnly UserPath As String = Exepath & "Users.psv"
    Public ReadOnly SettingsPath As String = Exepath & "Settings.psv"
    Public ReadOnly EventLogger As New LogEngine.LogEngine(Logpath, UserPath, "Efevid", True)
    Public ReadOnly SettingsProvider As New Settings(SettingsPath)
    Public Log_Filepath As String = Exepath & "VidLog.psv"
    Public User_filepath As String = Exepath & "Users.psv"
    Public User As String = "Efevid"
    Public reqpath As String = Exepath & DirSeparator & "req"
    Public reqfile As String = reqpath & DirSeparator & "efevid.runme"
    Public EventLogger As New LogEngine.LogEngine(Log_Filepath, User_filepath, User)

    Sub Main()
        Do While True
            If Not IO.Directory.Exists(reqpath) Then
                IO.Directory.CreateDirectory(reqpath)
            End If

            If IO.File.Exists(reqfile) Then
                Try
                    Dim statuspath As String = Exepath & "hfiles" & DirSeparator & "status.htm"
                    Dim ConfigFilePath As String = Exepath & "Config.cfg"
                    Try
                        Dim ESWikiBOT As Bot = New Bot(New ConfigFile(ConfigFilePath))
                        IO.File.WriteAllText(statuspath, My.Resources.status_loading)
                        Dim igen As New VideoGen(ESWikiBOT)
                        IO.File.WriteAllText(statuspath, My.Resources.status_gen)
                        igen.CheckEfe()
                        IO.File.WriteAllText(statuspath, My.Resources.status_OK)
                    Catch ex As Exception
                        IO.File.WriteAllText(statuspath, My.Resources.status_OK)
                    End Try
                Catch ex As Exception
                    EventLogger.EX_Log(ex.Message, "Checker")
                End Try
                Try
                    Threading.Thread.Sleep(3000)
                    IO.File.Delete(reqfile)
                    IO.File.Delete(Log_Filepath)
                    IO.File.Create(Log_Filepath).Close()
                Catch ex As Exception
                End Try
            End If
            System.Threading.Thread.Sleep(500)
        Loop

    End Sub




End Module
