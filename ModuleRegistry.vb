Imports Microsoft.Win32

Module ModuleRegistry
    Public Function myRegSetValue(myRegKey As String, myKeyValue As String) As Boolean
        Dim regSave As Boolean = False
        Dim regKey As RegistryKey
        Try
            regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", True)
            regKey.CreateSubKey("SnizenRu\VKPost")
            regKey.Close()

            regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\SnizenRu\VKPost", True)
            regKey.SetValue(myRegKey, myKeyValue)
            regKey.Close()

            regSave = True
        Catch
            regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", True)
            regKey.CreateSubKey("SnizenRu\VKPost")
            regKey.Close()

            regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\SnizenRu\VKPost", True)
            regKey.SetValue(myRegKey, myKeyValue)
            regKey.Close()
        End Try
        Return regSave
    End Function

    Public Function myRegGetValue(myRegKey As String, defValue As String) As String
        Dim regKey As RegistryKey
        Dim myKeyValue As String
        Try
            regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", True)
            regKey.CreateSubKey("SnizenRu\VKPost")
            regKey.Close()

            regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\SnizenRu\VKPost", True)
            myKeyValue = regKey.GetValue(myRegKey, defValue)
            regKey.Close()
        Catch
            regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", True)
            regKey.CreateSubKey("SnizenRu\VKPost")
            regKey.Close()

            regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\SnizenRu\VKPost", True)
            myKeyValue = regKey.GetValue(myRegKey, defValue)
            regKey.Close()
        End Try
        Return myKeyValue
    End Function

End Module
