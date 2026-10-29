Hedge 0.6.2

START
Run Hedge-Setup-0.6.2.exe. The installer installs for your Windows user,
creates Start menu and desktop shortcuts, and offers to start Hedge.
Windows 11 normally includes the required .NET Framework and its compiler.
The installer builds Hedge using your Windows framework during setup.
No separate SDK download or administrator rights are required. The app is unsigned; Windows may show a
publisher warning. Source code is installed in the Source folder.

The left drawer opens when Hedge starts. Look for the narrow edge strip halfway
down each screen edge after collapsing a drawer. Hover or click
to reveal the drawer. The tray icon has Settings and Exit. Use Pin to keep a
drawer open. 1 / 2 views switches between one and two views per drawer. Each
view has tabs. Stack / side changes how the two views are arranged.

SHORTCUTS AND TABS
The shortcut pane contains Home, Desktop, Downloads, Documents, OneDrive if
available, pinned folders, and your drives. Hedge identifies Google Drive from
its volume or network label, or a running Google Drive app for drive Z:.
Right-click a folder and choose Pin folder. Right-click a shortcut to remove a
pin. The + icon opens a tab; its x closes it. The small icons at the right open
Home, Drives, or show and hide shortcuts. A tab has its own path and search.

DRAG THE INNER EDGE
Drag the inside edge of either drawer to resize it. The left and right widths
are saved separately on exit. Click a column heading to sort and drag the
column borders to adjust widths. The compact buttons beside the address bar
are Back, Forward, Up, and a folder hierarchy menu. Press Enter in the address
bar to open a typed path.

SEARCH
Type a filename or partial name in Find. Toggle .* to use .NET regular
expressions; toggle Sub to search within the folder tree. The
first folder scan runs in the background; search uses its cache after it is
complete. F5 rebuilds the cache. The Folder column identifies each result's
location. Search is case-insensitive. Links and junctions are not followed.

COPY AND MOVE
Drag and drop between Hedge views and File Explorer. Drops are copied by
default; hold Shift to move and confirm the move. The pointer shows a prohibited
symbol for a folder itself, its current parent, or a folder inside itself.
Mixed selections are rejected if any selected item has an invalid destination.
Windows handles progress and file conflict dialogs. A canceled operation may
already have completed some items. Ctrl+C and Ctrl+V copy, F2 renames, and Delete
sends selected items to Windows' recycle operation with confirmation. On drives
without Recycle Bin support Windows may offer permanent deletion. New folder and
paste use the current view's folder; dropping onto a folder targets that folder.

SETTINGS AND UNINSTALL
Right-click the tray icon for Settings, to open a drawer, or to Exit. Exit the
app before updating it. Uninstall via Windows Settings > Apps > Installed apps.
Uninstall preserves settings and pinned locations in %LOCALAPPDATA%\Hedge.
No user files are removed by the installer or uninstaller.

VALIDATION
This installer compiles the app using Windows framework libraries, correcting
the earlier assembly mismatch. This build also fixes the installer selecting an unrelated System.dll. The installer and Windows UI have not been run
on Windows in the development environment. If compilation fails, setup shows
the error and writes %LOCALAPPDATA%\Hedge\install.log.
Search, Explorer drag and drop, Windows file dialogs and appearance require a
Windows test. Paths shown in the screenshot are examples from that Windows PC;
Hedge reads your actual account folders and drives.

TROUBLESHOOTING
If Hedge reports a startup error, its details are saved in
%LOCALAPPDATA%\Hedge\startup.log. Paste that location into the File Explorer
address bar, then share the error text. If you launch Hedge again while it
is already running, a message explains how to find it.

Previous EdgeFiles settings are read on first run when Hedge settings do not exist.
The two versions can be uninstalled separately.

TAB OVERFLOW
When tabs exceed the available space, use the small arrows at either end of
the tab strip. Click the x on a tab to close it. The collapsed hotspot is a
narrow, subdued strip at the screen edge.

APPEARANCE
Choose green or gray folder and shortcut icons in Settings.


GROUP JOB STRIPS
Each file group has a Transfers strip at the bottom. Click it to expand or collapse job details. Transfers involving that group appear in its list, including both ends of a drag between groups. Additional jobs queue and run one at a time. Queued jobs can be cancelled. Moves ask for confirmation inside the expanded job panel. Windows still handles replace, permission and provider questions; the panel marks detected Windows dialogs as needing attention and offers Show question. Recent job results remain available for this session.

DRAG AND DROP
Drop on a folder row, empty file-list area (the open folder), or a sidebar folder/drive.
Sidebar destinations include Home, Desktop, Downloads, Documents, OneDrive and pinned folders.
An available sidebar destination highlights while dragging. Empty sidebar space is not a target.
Default is Copy, even on the same drive. Shift requests Move and requires confirmation in the job panel.
Ctrl requests Copy. Unsupported source effects are rejected. Windows asks about existing names;
use Show question in the job panel to bring the native Replace/Skip question forward.

LIVE FOLDER UPDATES
Folder contents and the current search update from Windows filesystem notifications, without periodic polling.
Changes are combined in a bounded 400 ms window. Subfolder notifications follow the Sub setting.
Watcher errors trigger one rescan and monitoring restart. If monitoring cannot start, the status bar
shows a warning; F5, navigating or reopening the tab retries. Some network/cloud providers may not
supply reliable filesystem notifications; F5 remains available.
