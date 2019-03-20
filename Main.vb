Imports MWBot.net
Imports MWBot.net.WikiBot
Imports Utils.Utils

Module Main
    Public Log_Filepath As String = Exepath & "VidLog.psv"
    Public User_filepath As String = Exepath & "Users.psv"
    Public User As String = "Efevid"
    Public EventLogger As New LogEngine.LogEngine(Log_Filepath, User_filepath, User)

    Sub Main()
        Do While True
            For Each f As String In IO.Directory.GetFiles(Exepath)
                Dim ext As String() = f.Split("."c)
                If ext(ext.Count - 1) = "runme" Then
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
                        IO.File.Delete(f)
                        IO.File.Delete(Log_Filepath)
                        IO.File.Create(Log_Filepath).Close()
                    Catch ex As Exception
                    End Try
                    Exit For
                End If
            Next
            System.Threading.Thread.Sleep(500)
        Loop

    End Sub




End Module
