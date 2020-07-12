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
    Public ReadOnly Log_Filepath As String = Exepath & "VidLog.psv"
    Public ReadOnly User_filepath As String = Exepath & "Users.psv"
    Public ReadOnly User As String = "Efevid"
    Public ReadOnly reqpath As String = Exepath & DirSeparator & "req"
    Public ReadOnly reqfile As String = reqpath & DirSeparator & "efevid.runme"
    Public ReadOnly statuspath As String = Exepath & "hfiles" & DirSeparator & "status.htm"


    Sub Main()

        Dim setProv As New LogEngine.Settings(SettingsPath)





        Do While True
            If Not IO.Directory.Exists(Reqfolder) Then
                IO.Directory.CreateDirectory(Reqfolder)
            End If
            If Not IO.Directory.Exists(Hfolder) Then
                IO.Directory.CreateDirectory(Hfolder)
            End If
            If Not IO.Directory.Exists(ImgFolder) Then
                IO.Directory.CreateDirectory(ImgFolder)
            End If
            If Not IO.File.Exists(statuspath) Then
                IO.File.Create(statuspath).Close()
            End If

            If IO.File.Exists(reqfile) Then
                Try
                    Try
                        Dim ESWikiBOT As Bot = New Bot(ConfigFilePath, EventLogger)
                        IO.File.WriteAllText(statuspath, My.Resources.status_loading)
                        Dim igen As New VideoGen(ESWikiBOT)
                        IO.File.WriteAllText(statuspath, My.Resources.status_gen)
                        igen.CheckEfe()
                        IO.File.WriteAllText(statuspath, My.Resources.status_OK)
                    Catch ex As Exception
                        IO.File.WriteAllText(statuspath, My.Resources.status_OK)
                        EventLogger.EX_Log(ex.Message, "Checker")
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

                    EventLogger.EX_Log(ex.Message, "delfiles")
                End Try
            End If
            System.Threading.Thread.Sleep(500)
        Loop

    End Sub

End Module
