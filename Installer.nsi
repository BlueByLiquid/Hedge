Unicode true
!include "MUI2.nsh"
Name "Hedge 0.6.4"
OutFile "../Hedge-Setup-0.6.4.exe"
InstallDir "$LOCALAPPDATA\Programs\Hedge"
RequestExecutionLevel user
SetCompressor /SOLID lzma
ShowInstDetails nevershow
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\Hedge.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Launch Hedge"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Function .onInit
 retry:
 System::Call 'kernel32::OpenMutexW(i 0x00100000, i 0, w "Local\Hedge.v1") p .r0'
 ${If} $0 != 0
   System::Call 'kernel32::CloseHandle(p r0)'
   MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "Please exit Hedge from its system tray menu before installing this update." IDRETRY retry
   Abort
 ${EndIf}
 ReadRegDWORD $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
 ${If} $1 < 378389
   MessageBox MB_OK|MB_ICONSTOP "Hedge requires .NET Framework 4.5 or later. Windows 11 normally includes it."
   Abort
 ${EndIf}
FunctionEnd

Section "Install"
 DetailPrint "Preparing the Windows-compatible application..."
 InitPluginsDir
 SetOutPath "$PLUGINSDIR"
 File "Hedge.cs"
 File "Transfers.cs"
 File "Jobs.cs"
 File "Hedge.ico"
 StrCpy $4 "$WINDIR\Microsoft.NET\Framework64\v4.0.30319"
 StrCpy $0 "$4\csc.exe"
 IfFileExists "$0" compiler_found
 StrCpy $4 "$WINDIR\Microsoft.NET\Framework\v4.0.30319"
 StrCpy $0 "$4\csc.exe"
 IfFileExists "$0" compiler_found
 MessageBox MB_OK|MB_ICONSTOP "The Windows .NET Framework compiler was not found. No files were installed."
 Abort
 compiler_found:
 nsExec::ExecToStack /TIMEOUT=120000 '"$0" /noconfig /nologo /langversion:5 /codepage:65001 /target:winexe /win32icon:"$PLUGINSDIR\Hedge.ico" /optimize+ /out:"$PLUGINSDIR\Hedge.exe" /reference:"$4\System.dll" /reference:"$4\System.Core.dll" /reference:"$4\System.Windows.Forms.dll" /reference:"$4\System.Drawing.dll" /reference:"$4\System.Xml.dll" "$PLUGINSDIR\Hedge.cs" "$PLUGINSDIR\Transfers.cs" "$PLUGINSDIR\Jobs.cs"'
 Pop $1
 Pop $2
 ${If} $1 != 0
   CreateDirectory "$LOCALAPPDATA\Hedge"
   FileOpen $3 "$LOCALAPPDATA\Hedge\install.log" w
   FileWrite $3 "Windows compiler exit status: $1$\r$\n$2"
   FileClose $3
   MessageBox MB_OK|MB_ICONSTOP "Could not build Hedge for Windows. Setup will stop without replacing the existing app.$\r$\n$\r$\nDetails saved to:$\r$\n$LOCALAPPDATA\Hedge\install.log$\r$\n$\r$\n$2"
   Abort
 ${EndIf}
 IfFileExists "$PLUGINSDIR\Hedge.exe" build_exists
 MessageBox MB_OK|MB_ICONSTOP "The Windows compiler did not produce Hedge.exe. Setup will stop without replacing the existing app."
 Abort
 build_exists:
 SetOutPath "$INSTDIR"
 ClearErrors
 CopyFiles /SILENT "$PLUGINSDIR\Hedge.exe" "$INSTDIR"
 IfErrors 0 copy_ok
 MessageBox MB_OK|MB_ICONSTOP "Unable to copy the new application to $INSTDIR. Please close Hedge and try again."
 Abort
 copy_ok:
 File "README.txt"
 File "LUCIDE-LICENSE.txt"
 CreateDirectory "$INSTDIR\Source"
 SetOutPath "$INSTDIR\Source"
 File "Hedge.cs"
 File "Transfers.cs"
 File "Jobs.cs"
 File "Installer.nsi"
 File "Hedge.ico"
 SetOutPath "$INSTDIR"
 WriteUninstaller "$INSTDIR\Uninstall.exe"
 CreateDirectory "$SMPROGRAMS\Hedge"
 CreateShortcut "$SMPROGRAMS\Hedge\Hedge.lnk" "$INSTDIR\Hedge.exe"
 CreateShortcut "$SMPROGRAMS\Hedge\Uninstall Hedge.lnk" "$INSTDIR\Uninstall.exe"
 CreateShortcut "$DESKTOP\Hedge.lnk" "$INSTDIR\Hedge.exe"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Hedge" "DisplayName" "Hedge"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Hedge" "DisplayVersion" "0.6.4"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Hedge" "UninstallString" '"$INSTDIR\Uninstall.exe"'
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Hedge" "DisplayIcon" "$INSTDIR\Hedge.exe"
 WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Hedge" "NoModify" 1
 WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Hedge" "NoRepair" 1
SectionEnd

Function un.onInit
 retry:
 System::Call 'kernel32::OpenMutexW(i 0x00100000, i 0, w "Local\Hedge.v1") p .r0'
 ${If} $0 != 0
   System::Call 'kernel32::CloseHandle(p r0)'
   MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "Please exit Hedge from its system tray menu before uninstalling." IDRETRY retry
   Abort
 ${EndIf}
FunctionEnd
Section "Uninstall"
 Delete "$DESKTOP\Hedge.lnk"
 Delete "$SMPROGRAMS\Hedge\Hedge.lnk"
 Delete "$SMPROGRAMS\Hedge\Uninstall Hedge.lnk"
 RMDir "$SMPROGRAMS\Hedge"
 Delete "$INSTDIR\Source\Hedge.cs"
 Delete "$INSTDIR\Source\Transfers.cs"
 Delete "$INSTDIR\Source\Jobs.cs"
 Delete "$INSTDIR\Source\Installer.nsi"
 Delete "$INSTDIR\Source\Hedge.ico"
 RMDir "$INSTDIR\Source"
 Delete "$INSTDIR\Hedge.exe"
 Delete "$INSTDIR\README.txt"
 Delete "$INSTDIR\LUCIDE-LICENSE.txt"
 Delete "$INSTDIR\Uninstall.exe"
 RMDir "$INSTDIR"
 DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Hedge"
SectionEnd
