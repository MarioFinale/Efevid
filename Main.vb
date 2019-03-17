Imports MWBot.net
Imports MWBot.net.WikiBot
Imports Utils.Utils

Module Main
    Public ReadOnly Header As String = Exepath & "Res" & DirSeparator & "header.hres"
    Public ReadOnly Bottom As String = Exepath & "Res" & DirSeparator & "bottom.hres"
    Public ReadOnly Hfolder As String = Exepath & "hfiles" & DirSeparator
    Public ReadOnly ImgFolder As String = Exepath & "Images" & DirSeparator
    Public ReadOnly Reqfolder As String = Exepath & "req" & DirSeparator

    Sub Main()
        Do While True
            If Not IO.Directory.Exists(Reqfolder) Then
                IO.Directory.CreateDirectory(Reqfolder)
            End If

            If IO.File.Exists(Reqfolder & "efevid.runme") Then
                IO.File.Delete(Reqfolder & "efevid.runme")
                Try
                    Log_Filepath = Exepath & "VidLog.psv"
                    Try
                        Dim ESWikiBOT As Bot = New Bot(New ConfigFile(ConfigFilePath), Log_Filepath)
                        Dim igen As New VideoGen(ESWikiBOT)
                        igen.CheckEfe()
                    Catch ex As Exception
                    End Try
                    Threading.Thread.Sleep(6000)
                    If IO.File.Exists(Exepath & "VidLog.psv") Then
                        IO.File.Delete(Exepath & "VidLog.psv")
                        IO.File.Create(Exepath & "VidLog.psv").Close()
                    End If
                Catch ex As Exception
                    EventLogger.EX_Log(ex.Message, "Checker")
                End Try
            End If
            Threading.Thread.Sleep(3000)
        Loop
    End Sub
End Module
