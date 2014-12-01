Main

Sub Main()

   Dim WshShell
   Set WshShell = WScript.CreateObject("WScript.Shell")

   Dim fso
   Set fso = WScript.CreateObject("Scripting.FileSystemObject")

   Dim myDir
   myDir = fso.GetParentFolderName(WScript.ScriptFullName)

   Dim ranu
   ranu = WScript.Arguments(0)

   Dim regRoot
   regRoot = WScript.Arguments(1)
   If Right(regRoot, 1) = "\" Then
      regRoot = Left(regRoot, Len(regRoot) - 1)
   End If
   If (ranu = "No") Then
      regRoot = "HKEY_LOCAL_MACHINE\" & regRoot
   Else
      regRoot = "HKEY_CURRENT_USER\" & regRoot
   End If

   Dim codebase
   codebase = WScript.Arguments(2)

   Dim regFile
   Dim genRegFile
   Dim regFileContents
   on error resume next
   Set regFile = fso.OpenTextFile(myDir & "\JetDdexProvider.reg", 1)
   if err then
		WScript.Echo "Cannot open " & myDir & "\ExtendedProvider.reg"
		WScript.Quit 1
   end if
   on error goto 0
   Set genRegFile = fso.CreateTextFile(myDir & "\JetDdexProvider.gen.reg", true)
   regFileContents = regFile.ReadAll()
   regFileContents = Replace(regFileContents, "%REGROOT%", regRoot)
   regFileContents = Replace(regFileContents, "%PROVIDERGUID%", "{52C271ED-FAE1-444E-8C3A-6DFEC4C3A974}")
   regFileContents = Replace(regFileContents, "%CODEBASE%", Replace(codebase, "\", "\\"))
   genRegFile.Write(regFileContents)
   genRegFile.Close()
   regFile.Close()

   Dim oExec
   Set oExec = WshShell.Exec(WScript.Arguments(3) & " /s """ & myDir & "\JetDdexProvider.gen.reg""")
   Do While oExec.Status = 0
      WScript.Sleep(100)
   Loop

   WScript.Echo ReadAllFromAny(oExec)
   
   fso.DeleteFile(myDir & "\JetDdexProvider.gen.reg")

End Sub

Function ReadAllFromAny(oExec)

	 Dim retVal
	 
	 retVal = ""

     If Not oExec.StdOut.AtEndOfStream Then
          retVal = retVal + oExec.StdOut.ReadAll
		  retVal = retVal + vbCRLF
     End If

     If Not oExec.StdErr.AtEndOfStream Then
          retVal = retVal + "STDERR: " + oExec.StdErr.ReadAll
     End If

	 ReadAllFromAny = retVal
	 
End Function