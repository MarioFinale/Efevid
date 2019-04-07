Imports MWBot.net
Imports MWBot.net.WikiBot
Imports Utils.Utils
Imports LogEngine

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
    Public ReadOnly EventLogger As New LogEngine.LogEngine(Logpath, UserPath, "Efevid")
    Public ReadOnly SettingsProvider As New Settings(SettingsPath)

    Sub Main()
        Dim ESWikiBOT As Bot = New Bot(ConfigFilePath, Logpath)
        Do While True
            If Not IO.Directory.Exists(Reqfolder) Then
                IO.Directory.CreateDirectory(Reqfolder)
            End If

            If IO.File.Exists(Reqfolder & "efevid.runme") Then
                IO.File.Delete(Reqfolder & "efevid.runme")
                Try
                    Try

                        Dim igen As New VideoGen(ESWikiBOT)
                        igen.CheckEfe()
                    Catch ex As Exception
                    End Try
                    Threading.Thread.Sleep(6000)
                    If IO.File.Exists(Logpath) Then
                        IO.File.Delete(Logpath)
                        IO.File.Create(Logpath).Close()
                    End If
                Catch ex As Exception
                    EventLogger.EX_Log(ex.Message, "Checker")
                End Try
            End If
            Threading.Thread.Sleep(3000)
        Loop
    End Sub
End Module
