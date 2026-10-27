using System;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using Timer = System.Windows.Forms.Timer;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

namespace Hedge {
 public class Settings {
  public int Width=700, SpotPercent=50, SpotHeight=100, Delay=250, Monitor=0;
  public bool LeftSplit=true,RightSplit=true,Hidden=false;
  public List<string> Favorites=new List<string>();
  public List<string> LeftA=new List<string>(),LeftB=new List<string>(),RightA=new List<string>(),RightB=new List<string>();
  public static string FilePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Hedge","settings.xml");
  public static Settings Load(){try{string legacy=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EdgeFiles","settings.xml");string source=File.Exists(FilePath)?FilePath:legacy;using(var s=File.OpenRead(source))return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(s);}catch{return new Settings();}}
  public void Save(){Directory.CreateDirectory(Path.GetDirectoryName(FilePath));string temp=FilePath+".tmp";using(var s=File.Create(temp))new XmlSerializer(typeof(Settings)).Serialize(s,this);if(File.Exists(FilePath))File.Replace(temp,FilePath,null);else File.Move(temp,FilePath);}
 }
 static class Program {
  public static bool Dragging,Busy; public static Settings Config;
  static string LogPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Hedge","startup.log");}}
  public static void Log(string message){try{Directory.CreateDirectory(Path.GetDirectoryName(LogPath));File.AppendAllText(LogPath,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" "+message+Environment.NewLine);}catch{}}
  static void Report(Exception error){string details=error==null?"Unknown error":error.ToString();Log("ERROR: "+details);try{MessageBox.Show("Hedge encountered a problem:\n\n"+(error==null?"Unknown error":error.Message)+"\n\nDetails were saved to:\n"+LogPath,"Hedge error",MessageBoxButtons.OK,MessageBoxIcon.Error);}catch{try{NativeMessage("Hedge could not start. Details were saved to "+LogPath+"\n\n"+details,"Hedge error",0x10);}catch{}}}
  [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="MessageBoxW")]static extern int NativeMessage(IntPtr parent,string text,string caption,uint type);
  static int NativeMessage(string text,string caption,uint type){return NativeMessage(IntPtr.Zero,text,caption,type);}
  [STAThread] static void Main(){
   Log("Starting Hedge 0.3.3");
   try{
    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
    Application.ThreadException+=(sender,args)=>Report(args.Exception);
    AppDomain.CurrentDomain.UnhandledException+=(sender,args)=>{var e=args.ExceptionObject as Exception;if(e!=null)Report(e);else Log("Unhandled error: "+args.ExceptionObject);};
    bool created;using(var mutex=new System.Threading.Mutex(true,"Local\\Hedge.v1",out created)){
     if(!created){Log("Another instance is running.");MessageBox.Show("Hedge is already running. Use the blue screen-edge strip or the Hedge icon in the system tray to open a drawer.","Hedge",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
     Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Config=Settings.Load();
     if(Config.Favorites==null)Config.Favorites=new List<string>();if(Config.LeftA==null)Config.LeftA=new List<string>();if(Config.LeftB==null)Config.LeftB=new List<string>();if(Config.RightA==null)Config.RightA=new List<string>();if(Config.RightB==null)Config.RightB=new List<string>();
     Log("Settings loaded; creating drawers.");var host=new Host();Log("Drawers created; showing the left drawer.");host.ShowInitial();Application.Run(host);Log("Exit completed.");
    }
   }catch(Exception error){Report(error);}
  }
  public static Button Button(string text,Action action){var b=new Button{Text=text,AutoSize=true,Height=29,FlatStyle=FlatStyle.System};b.Click+=(s,e)=>action();return b;}
  public static void Theme(Control c){
   c.BackColor=Color.FromArgb(30,32,38);c.ForeColor=Color.FromArgb(230,233,240);
   var button=c as Button;if(button!=null){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=Color.FromArgb(62,67,79);button.FlatAppearance.MouseOverBackColor=Color.FromArgb(57,69,89);button.BackColor=Color.FromArgb(42,46,55);}
   var tb=c as TextBox;if(tb!=null){tb.BackColor=Color.FromArgb(42,46,55);tb.ForeColor=Color.White;tb.BorderStyle=BorderStyle.FixedSingle;}
   var lv=c as ListView;if(lv!=null){lv.BackColor=Color.FromArgb(27,29,34);lv.ForeColor=Color.FromArgb(232,235,241);lv.GridLines=false;}
   var lb=c as ListBox;if(lb!=null){lb.BackColor=Color.FromArgb(35,38,45);lb.ForeColor=Color.FromArgb(230,233,240);lb.BorderStyle=BorderStyle.None;}
   foreach(Control child in c.Controls)Theme(child);
  }
  public static void Error(Exception e){MessageBox.Show(e.Message,"Hedge",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
 }
 class Host:ApplicationContext {
  Drawer left,right;NotifyIcon tray;bool exiting;
  public Host(){left=new Drawer(true);right=new Drawer(false);tray=new NotifyIcon{Text="Hedge • left and right file drawers",Icon=SystemIcons.Application,Visible=true};var menu=new ContextMenuStrip();menu.Items.Add("Open left",null,(s,e)=>left.Reveal());menu.Items.Add("Open right",null,(s,e)=>right.Reveal());menu.Items.Add("Settings",null,(s,e)=>EditSettings());menu.Items.Add("Exit",null,(s,e)=>ExitThread());tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>left.Reveal();left.Show();right.Show();left.Collapse();right.Collapse();}
  public void ShowInitial(){left.Reveal();}
  void EditSettings(){using(var f=new Form{Text="Hedge settings",Size=new Size(450,430),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){var table=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=8};table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));f.Controls.Add(table);var c=Program.Config;
  var width=Number(table,"Drawer width (pixels)",c.Width,420,1400);var pos=Number(table,"Hotspot position (% down edge)",c.SpotPercent,5,95);var height=Number(table,"Hotspot height (pixels)",c.SpotHeight,30,600);var delay=Number(table,"Hover delay (milliseconds)",c.Delay,0,2000);var monitor=Number(table,"Monitor (1 = first display)",c.Monitor+1,1,Screen.AllScreens.Length);var hidden=new CheckBox{Text="Show hidden files",Checked=c.Hidden,AutoSize=true};table.Controls.Add(hidden);table.SetColumnSpan(hidden,2);table.Controls.Add(new Label{Text="Edge strips stay visible. Drag files onto a strip to open its drawer.",AutoSize=true});table.SetColumnSpan(table.Controls[table.Controls.Count-1],2);
  table.Controls.Add(Program.Button("Save",()=>{c.Width=(int)width.Value;c.SpotPercent=(int)pos.Value;c.SpotHeight=(int)height.Value;c.Delay=(int)delay.Value;c.Monitor=(int)monitor.Value-1;c.Hidden=hidden.Checked;left.Position();right.Position();left.RefreshAll();right.RefreshAll();Save();f.Close();}));f.ShowDialog();}}
  NumericUpDown Number(TableLayoutPanel t,string label,int value,int min,int max){t.Controls.Add(new Label{Text=label,AutoSize=true,Anchor=AnchorStyles.Left});var n=new NumericUpDown{Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),Dock=DockStyle.Fill};t.Controls.Add(n);return n;}
  void Save(){try{left.Store();right.Store();Program.Config.Save();}catch(Exception e){Program.Error(e);}}
  protected override void ExitThreadCore(){if(exiting)return;if(Program.Busy||Program.Dragging){MessageBox.Show("Wait for the current file operation to finish before exiting.","Hedge");return;}exiting=true;Save();tray.Visible=false;tray.Dispose();left.Shutdown();right.Shutdown();base.ExitThreadCore();}
 }
 class Drawer:Form {
  bool left,expanded,closing,pinned,fit;DateTime hover=DateTime.MinValue,away=DateTime.MinValue;
  SplitContainer split;TabPane a,b;Form strip;Timer timer;Button pin;public Drawer(bool isLeft){left=isLeft;Text="Hedge • "+(left?"Left":"Right");FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.FromArgb(228,232,238);Padding=new Padding(2);Font=new Font("Segoe UI",9);
   var header=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,Padding=new Padding(3),WrapContents=false};header.Controls.Add(new Label{Text=left?"LEFT FILES":"RIGHT FILES",AutoSize=true,Margin=new Padding(5,8,12,0)});pin=Program.Button("Pin",()=>{pinned=!pinned;pin.Text=pinned?"Unpin":"Pin";});header.Controls.Add(pin);header.Controls.Add(Program.Button("1 / 2 views",()=>{split.Panel2Collapsed=!split.Panel2Collapsed;}));header.Controls.Add(Program.Button("Stack / side",()=>{split.Orientation=split.Orientation==Orientation.Horizontal?Orientation.Vertical:Orientation.Horizontal;}));header.Controls.Add(Program.Button("Fit columns",()=>{fit=!fit;a.SetFit(fit);b.SetFit(fit);Position();a.Refit();b.Refit();}));header.Controls.Add(Program.Button("Collapse",()=>Collapse()));
   split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,Size=new Size(700,650),SplitterWidth=6,Panel1MinSize=150,Panel2MinSize=150,SplitterDistance=320};a=new TabPane(left?Program.Config.LeftA:Program.Config.RightA);b=new TabPane(left?Program.Config.LeftB:Program.Config.RightB);split.Panel1.Controls.Add(a);split.Panel2.Controls.Add(b);split.Panel2Collapsed=!(left?Program.Config.LeftSplit:Program.Config.RightSplit);Controls.Add(split);Controls.Add(header);Program.Theme(this);
   strip=new Form{FormBorderStyle=FormBorderStyle.None,ShowInTaskbar=false,TopMost=true,BackColor=Color.FromArgb(64,125,200),Opacity=.8,AllowDrop=true};strip.Cursor=Cursors.Hand;strip.MouseEnter+=(s,e)=>hover=DateTime.UtcNow;strip.MouseLeave+=(s,e)=>hover=DateTime.MinValue;strip.Click+=(s,e)=>Reveal();strip.DragEnter+=(s,e)=>{if(e.Data.GetDataPresent(DataFormats.FileDrop)){e.Effect=DragDropEffects.Copy;Reveal();}};
   timer=new Timer{Interval=40};timer.Tick+=(s,e)=>TickEdge();timer.Start();SystemEventsHook();Position();strip.Show();FormClosing+=(s,e)=>{if(!closing){e.Cancel=true;Collapse();}};
  }
  void SystemEventsHook(){Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplayChanged;}
  void DisplayChanged(object s,EventArgs e){if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)Position);}
  public void Position(){var screens=Screen.AllScreens;var screen=screens[Math.Max(0,Math.Min(Program.Config.Monitor,screens.Length-1))];var r=screen.WorkingArea;int w=fit?r.Width:Math.Min(Program.Config.Width,r.Width);Bounds=new Rectangle(left?r.Left:r.Right-w,r.Top,w,r.Height);int h=Math.Min(Program.Config.SpotHeight,r.Height);int y=r.Top+(int)((r.Height-h)*Program.Config.SpotPercent/100.0);strip.Bounds=new Rectangle(left?r.Left:r.Right-5,y,5,h);}
  void TickEdge(){if(!expanded){if(hover!=DateTime.MinValue&&(DateTime.UtcNow-hover).TotalMilliseconds>=Program.Config.Delay)Reveal();return;}if(pinned||Program.Dragging||Program.Busy||ContainsFocus||Bounds.Contains(Cursor.Position)){away=DateTime.MinValue;return;}if(away==DateTime.MinValue)away=DateTime.UtcNow;if((DateTime.UtcNow-away).TotalMilliseconds>700)Collapse();}
  public void Reveal(){if(expanded)return;expanded=true;away=DateTime.MinValue;Position();Show();BringToFront();strip.Hide();}
  public void Collapse(){expanded=false;Hide();hover=DateTime.MinValue;strip.Show();}
  public void RefreshAll(){a.RefreshActive();b.RefreshActive();}
  public void Store(){if(left){Program.Config.LeftA=a.Paths();Program.Config.LeftB=b.Paths();Program.Config.LeftSplit=!split.Panel2Collapsed;}else{Program.Config.RightA=a.Paths();Program.Config.RightB=b.Paths();Program.Config.RightSplit=!split.Panel2Collapsed;}}
  public void Shutdown(){closing=true;Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplayChanged;timer.Stop();timer.Dispose();strip.Close();strip.Dispose();Close();Dispose();}
 }
 class TabPane:UserControl {
  TabControl tabs;bool fit;public TabPane(List<string> paths){Dock=DockStyle.Fill;tabs=new TabControl{Dock=DockStyle.Fill,DrawMode=TabDrawMode.OwnerDrawFixed,SizeMode=TabSizeMode.Fixed,ItemSize=new Size(120,25)};tabs.DrawItem+=(ss,ee)=>{bool active=ee.Index==tabs.SelectedIndex;using(var brush=new SolidBrush(active?Color.FromArgb(55,66,83):Color.FromArgb(38,41,48)))ee.Graphics.FillRectangle(brush,ee.Bounds);TextRenderer.DrawText(ee.Graphics,tabs.TabPages[ee.Index].Text,tabs.Font,ee.Bounds,Color.FromArgb(232,235,241),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=32,WrapContents=false};bar.Controls.Add(Program.Button("+ Tab",()=>Add(Current==null?Environment.GetFolderPath(Environment.SpecialFolder.UserProfile):Current.PathNow)));bar.Controls.Add(Program.Button("Close tab",()=>{if(tabs.TabPages.Count>1){var p=tabs.SelectedTab;tabs.TabPages.Remove(p);p.Dispose();}}));bar.Controls.Add(Program.Button("Home",()=>{if(Current!=null)Current.Navigate(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));}));bar.Controls.Add(Program.Button("Drives",()=>ShowDrives(bar)));bar.Controls.Add(Program.Button("Places",()=>{if(Current!=null)Current.TogglePlaces();}));Controls.Add(tabs);Controls.Add(bar);if(paths.Count==0)Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));else foreach(var path in paths)Add(Directory.Exists(path)?path:Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));tabs.SelectedIndexChanged+=(s,e)=>RefreshActive();}
  FileView Current{get{return tabs.SelectedTab==null?null:tabs.SelectedTab.Controls[0] as FileView;}}
  public void SetFit(bool fit){this.fit=fit;foreach(TabPage p in tabs.TabPages)((FileView)p.Controls[0]).SetFit(fit);}
  public void Refit(){if(Current!=null)Current.FitColumns();}
  void ShowDrives(Control owner){var m=new ContextMenuStrip();foreach(var drive in DriveInfo.GetDrives()){string path=drive.Name;m.Items.Add(path,null,(s,e)=>Current.Navigate(path));}m.Closed+=(s,e)=>m.Dispose();m.Show(owner,0,owner.Height);}
  void Add(string path){var page=new TabPage();var view=new FileView(path);view.SetFit(fit);view.PathChanged+=()=>{page.Text=Short(view.PathNow);page.ToolTipText=view.PathNow;};page.Controls.Add(view);tabs.TabPages.Add(page);tabs.SelectedTab=page;page.Text=Short(path);}
  string Short(string path){string name=System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar));return name.Length==0?path:name;}
  public List<string> Paths(){return tabs.TabPages.Cast<TabPage>().Select(p=>((FileView)p.Controls[0]).PathNow).ToList();}
  public void RefreshActive(){if(Current!=null)Current.RefreshFiles();}
 }
 class Entry {public string Path,Name,Kind;public bool Folder;public long Size;public DateTime Modified;public FileAttributes Attributes;}
 class EntrySort:IComparer {public int Column;public bool Descending;public int Compare(object x,object y){var a=(Entry)x;var b=(Entry)y;if(a.Folder!=b.Folder)return a.Folder?-1:1;int n=Column==2?a.Size.CompareTo(b.Size):Column==3?a.Modified.CompareTo(b.Modified):StringComparer.CurrentCultureIgnoreCase.Compare(Column==1?a.Kind:Column==4?a.Attributes.ToString():Column==5?System.IO.Path.GetDirectoryName(a.Path):a.Name,Column==1?b.Kind:Column==4?b.Attributes.ToString():Column==5?System.IO.Path.GetDirectoryName(b.Path):b.Name);return Descending?-n:n;}}
 class Place {public string Label,Path;public override string ToString(){return Label;}}
 static class KnownPlaces {
  [DllImport("shell32.dll")]static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id,uint flags,IntPtr token,out IntPtr path);
  public static string Downloads(){IntPtr p=IntPtr.Zero;try{if(SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"),0,IntPtr.Zero,out p)==0)return Marshal.PtrToStringUni(p);}catch{}finally{if(p!=IntPtr.Zero)Marshal.FreeCoTaskMem(p);}return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads");}
 }
 static class FileOps {
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct SHFILEOPSTRUCT{public IntPtr hwnd;public uint wFunc;[MarshalAs(UnmanagedType.LPWStr)]public string pFrom;[MarshalAs(UnmanagedType.LPWStr)]public string pTo;public ushort fFlags;[MarshalAs(UnmanagedType.Bool)]public bool fAnyOperationsAborted;public IntPtr hNameMappings;[MarshalAs(UnmanagedType.LPWStr)]public string lpszProgressTitle;}
  [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHFileOperation(ref SHFILEOPSTRUCT op);
  public static void Execute(string[] paths,string target,uint operation){var op=new SHFILEOPSTRUCT{wFunc=operation,pFrom=String.Join("\0",paths)+"\0\0",pTo=target==null?null:target+"\0\0",fFlags=operation==3?(ushort)0x40:(ushort)0};int code=SHFileOperation(ref op);if(op.fAnyOperationsAborted)throw new OperationCanceledException();if(code!=0)throw new IOException("Windows file operation failed (code "+code+").");}
 }
 class FileView:UserControl {
  public string PathNow;public event Action PathChanged;TextBox address,filter;CheckBox regex,subfolders;ListView list;Label status;Stack<string> back=new Stack<string>(),forward=new Stack<string>();EntrySort sorter=new EntrySort();List<Entry> entries=new List<Entry>();int generation,searchGeneration;bool scanning,fit;int skipped;ListBox places;Panel placesPanel;List<Entry> visible=new List<Entry>();CancellationTokenSource scanCancel,searchCancel;FileSystemWatcher watcher;Timer debounce,searchDelay;
  public FileView(string path){Dock=DockStyle.Fill;PathNow=path;var nav=new TableLayoutPanel{Dock=DockStyle.Top,Height=36,ColumnCount=5};nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,42));nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,42));nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,42));nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,54));nav.Controls.Add(Program.Button("<",()=>History(back,forward)));nav.Controls.Add(Program.Button(">",()=>History(forward,back)));nav.Controls.Add(Program.Button("Up",()=>{var p=Directory.GetParent(PathNow);if(p!=null)Navigate(p.FullName);}));address=new TextBox{Dock=DockStyle.Fill,Text=path,Margin=new Padding(3,6,3,3)};address.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){Navigate(Environment.ExpandEnvironmentVariables(address.Text));e.SuppressKeyPress=true;}};nav.Controls.Add(address);nav.Controls.Add(Program.Button("Go",()=>Navigate(Environment.ExpandEnvironmentVariables(address.Text))));
   filter=new TextBox{Dock=DockStyle.Fill};
   regex=new CheckBox{Text="Regex",AutoSize=true};subfolders=new CheckBox{Text="Include subfolders",AutoSize=true};
   var searchBar=new TableLayoutPanel{Dock=DockStyle.Top,Height=34,ColumnCount=4,Padding=new Padding(2)};
   searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,48));searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
   searchBar.Controls.Add(new Label{Text="Find",AutoSize=true,Margin=new Padding(0,5,0,0)});searchBar.Controls.Add(filter);searchBar.Controls.Add(regex);searchBar.Controls.Add(subfolders);
   searchDelay=new Timer{Interval=120};searchDelay.Tick+=(s,e)=>{searchDelay.Stop();Render();};filter.TextChanged+=(s,e)=>SearchChanged();regex.CheckedChanged+=(s,e)=>SearchChanged();subfolders.CheckedChanged+=(s,e)=>{searchDelay.Stop();RefreshFiles();};
   list=new ListView{Dock=DockStyle.Fill,ShowItemToolTips=true,View=View.Details,FullRowSelect=true,GridLines=true,HideSelection=false,MultiSelect=true,AllowDrop=true};list.Columns.Add("Name",245);list.Columns.Add("Type",95);list.Columns.Add("Size",90,HorizontalAlignment.Right);list.Columns.Add("Modified",145);list.Columns.Add("Attributes",100);list.Columns.Add("Folder",240);list.VirtualMode=true;list.RetrieveVirtualItem+=(s,e)=>{e.Item=e.ItemIndex<visible.Count?MakeRow(visible[e.ItemIndex]):new ListViewItem("");};list.ColumnClick+=(s,e)=>{sorter.Descending=sorter.Column==e.Column&&!sorter.Descending;sorter.Column=e.Column;Render();};list.DoubleClick+=(s,e)=>OpenSelected();list.ItemDrag+=(s,e)=>StartDrag();list.DragEnter+=HandleDragOver;list.DragOver+=HandleDragOver;list.DragDrop+=Drop;list.KeyDown+=KeysDown;
   placesPanel=new Panel{Dock=DockStyle.Left,Width=152,Padding=new Padding(6,4,6,4)};places=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false};var placesHeader=new FlowLayoutPanel{Dock=DockStyle.Top,Height=30,WrapContents=false};placesHeader.Controls.Add(new Label{Text="PLACES",AutoSize=true,Margin=new Padding(2,7,4,0)});placesHeader.Controls.Add(Program.Button("+",()=>PinCurrent()));placesPanel.Controls.Add(places);placesPanel.Controls.Add(placesHeader);places.MouseUp+=(ss,ee)=>{if(ee.Button==MouseButtons.Left){var place=places.SelectedItem as Place;if(place!=null)Navigate(place.Path);}};Place contextPlace=null;places.MouseDown+=(ss,ee)=>{if(ee.Button==MouseButtons.Right){int index=places.IndexFromPoint(ee.Location);contextPlace=index>=0?places.Items[index] as Place:null;}};var placeMenu=new ContextMenuStrip();placeMenu.Items.Add("Pin current folder",null,(ss,ee)=>PinCurrent());placeMenu.Items.Add("Remove pinned folder",null,(ss,ee)=>{var item=contextPlace;if(item!=null){Program.Config.Favorites.RemoveAll(x=>String.Equals(x,item.Path,StringComparison.OrdinalIgnoreCase));FillPlaces();}});places.ContextMenuStrip=placeMenu;FillPlaces();
   var menu=new ContextMenuStrip();menu.Items.Add("Open",null,(s,e)=>OpenSelected());menu.Items.Add("Open in File Explorer",null,(s,e)=>{System.Diagnostics.Process.Start("explorer.exe",PathNow);});menu.Items.Add("Copy",null,(s,e)=>Copy());menu.Items.Add("Paste",null,(s,e)=>Paste());menu.Items.Add("Rename",null,(s,e)=>Rename());menu.Items.Add("New folder",null,(s,e)=>NewFolder());menu.Items.Add("Recycle selected",null,(s,e)=>Recycle());menu.Items.Add("Refresh",null,(s,e)=>RefreshFiles());list.ContextMenuStrip=menu;list.SizeChanged+=(ss,ee)=>{if(fit)FitColumns();};
   status=new Label{Dock=DockStyle.Bottom,Height=25,Text="Loading...",Padding=new Padding(4)};Controls.Add(list);Controls.Add(placesPanel);Controls.Add(status);Controls.Add(searchBar);Controls.Add(nav);debounce=new Timer{Interval=400};debounce.Tick+=(s,e)=>{debounce.Stop();RefreshFiles();};Load+=(s,e)=>RefreshFiles();Disposed+=(s,e)=>{generation++;searchGeneration++;if(scanCancel!=null)scanCancel.Cancel();if(searchCancel!=null)searchCancel.Cancel();if(watcher!=null)watcher.Dispose();debounce.Dispose();searchDelay.Dispose();};Program.Theme(this);}
  public void TogglePlaces(){placesPanel.Visible=!placesPanel.Visible;if(fit)FitColumns();}
  void PinCurrent(){if(!Program.Config.Favorites.Any(x=>String.Equals(x,PathNow,StringComparison.OrdinalIgnoreCase)))Program.Config.Favorites.Add(PathNow);FillPlaces();}
  void AddPlace(string name,string path){if(!String.IsNullOrEmpty(path)&&Directory.Exists(path))places.Items.Add(new Place{Label=name,Path=path});}
  void FillPlaces(){places.Items.Clear();AddPlace("Home",Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));AddPlace("Desktop",Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));AddPlace("Downloads",KnownPlaces.Downloads());AddPlace("Documents",Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));AddPlace("Pictures",Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));AddPlace("Music",Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));AddPlace("Videos",Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));AddPlace("OneDrive",Environment.GetEnvironmentVariable("OneDrive"));foreach(var item in Program.Config.Favorites.ToArray())AddPlace("★ "+(System.IO.Path.GetFileName(item.TrimEnd(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar))==""?item:System.IO.Path.GetFileName(item.TrimEnd(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar))),item);foreach(var drive in DriveInfo.GetDrives())AddPlace("Drive "+drive.Name,drive.Name);}
  public void SetFit(bool enabled){fit=enabled;if(list!=null)FitColumns();}
  public void FitColumns(){if(list==null)return;if(!fit){int[] widths={245,95,90,145,100,240};for(int i=0;i<widths.Length;i++)list.Columns[i].Width=widths[i];return;}
   string[] samples={"Name","Type","888.8 GB","2026-10-26 00:00","Attributes","Folder"};foreach(var e in visible){string[] values={e.Name,e.Kind,"888.8 GB","2026-10-26 00:00",e.Attributes.ToString(),System.IO.Path.GetDirectoryName(e.Path)??""};for(int i=0;i<samples.Length;i++)if(values[i].Length>samples[i].Length)samples[i]=values[i];}
   int[] width=new int[6];int total=0;for(int i=0;i<width.Length;i++){width[i]=Math.Min(6000,Math.Max(65,TextRenderer.MeasureText(samples[i],list.Font).Width+24));total+=width[i];}
   int available=Math.Max(6,list.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-6);if(total>available){double ratio=(double)available/total;int used=0;for(int i=0;i<width.Length;i++){width[i]=Math.Max(1,(int)(width[i]*ratio));used+=width[i];}width[0]+=Math.Max(0,available-used);}
   for(int i=0;i<width.Length;i++)list.Columns[i].Width=width[i];

  }
  void History(Stack<string> from,Stack<string> to){if(from.Count==0)return;string path=from.Pop();if(!Directory.Exists(path))return;to.Push(PathNow);Navigate(path,false);}
  public void Navigate(string path){Navigate(path,true);}
  void Navigate(string path,bool remember){try{path=System.IO.Path.GetFullPath(path);if(!Directory.Exists(path))throw new IOException("Folder not found: "+path);if(remember&&!String.Equals(path,PathNow,StringComparison.OrdinalIgnoreCase)){back.Push(PathNow);forward.Clear();}PathNow=path;address.Text=path;if(PathChanged!=null)PathChanged();RefreshFiles();}catch(Exception e){Program.Error(e);}}
  public async void RefreshFiles(){
   if(IsDisposed)return;int version=++generation;if(scanCancel!=null)scanCancel.Cancel();if(searchCancel!=null)searchCancel.Cancel();searchGeneration++;
   var cancellation=new CancellationTokenSource();scanCancel=cancellation;var token=cancellation.Token;string path=PathNow;bool hidden=Program.Config.Hidden,recursive=subfolders.Checked;
   scanning=true;skipped=0;entries=new List<Entry>();visible=new List<Entry>();list.VirtualListSize=0;status.Text="Reading "+path+"...";
   Watch(path);
   try{
    var result=await Task.Run(()=>ReadEntries(path,recursive,hidden,token),token);
    if(IsDisposed||version!=generation)return;entries=result.Items;skipped=result.Skipped;scanning=false;Render();
   }catch(OperationCanceledException){}catch(Exception e){if(!IsDisposed&&version==generation){scanning=false;status.Text=e.Message;}}
   finally{if(scanCancel==cancellation)scanCancel=null;cancellation.Dispose();}
  }
  class ScanResult {public List<Entry> Items=new List<Entry>();public int Skipped;}
  static ScanResult ReadEntries(string path,bool recursive,bool hidden,CancellationToken token){
   var result=new ScanResult();var pending=new Stack<string>();pending.Push(path);
   while(pending.Count>0){token.ThrowIfCancellationRequested();string folder=pending.Pop();
    try{foreach(var item in new DirectoryInfo(folder).EnumerateFileSystemInfos()){
      token.ThrowIfCancellationRequested();try{
       var attrs=item.Attributes;if(!hidden&&(attrs&FileAttributes.Hidden)!=0)continue;bool dir=(attrs&FileAttributes.Directory)!=0;
       result.Items.Add(new Entry{Path=item.FullName,Name=item.Name,Folder=dir,Size=dir?0:((FileInfo)item).Length,Modified=item.LastWriteTime,Kind=dir?"Folder":item.Extension.ToUpperInvariant()+" file",Attributes=attrs});
       if(recursive&&dir&&(attrs&FileAttributes.ReparsePoint)==0)pending.Push(item.FullName);
      }catch(UnauthorizedAccessException){result.Skipped++;}catch(IOException){result.Skipped++;}catch(System.Security.SecurityException){result.Skipped++;}
     }
    }catch(UnauthorizedAccessException){result.Skipped++;}catch(IOException){result.Skipped++;}catch(System.Security.SecurityException){result.Skipped++;}
   }return result;
  }
  void Watch(string path){try{if(watcher!=null)watcher.Dispose();watcher=new FileSystemWatcher(path){IncludeSubdirectories=subfolders.Checked,NotifyFilter=NotifyFilters.FileName|NotifyFilters.DirectoryName|NotifyFilters.LastWrite|NotifyFilters.Size};FileSystemEventHandler change=(s,e)=>QueueRefresh();watcher.Changed+=change;watcher.Created+=change;watcher.Deleted+=change;watcher.Renamed+=(s,e)=>QueueRefresh();watcher.Error+=(s,e)=>QueueRefresh();watcher.EnableRaisingEvents=true;}catch{}}
  void QueueRefresh(){if(IsDisposed||!IsHandleCreated)return;try{BeginInvoke((Action)(()=>{if(IsDisposed)return;debounce.Stop();debounce.Start();}));}catch(InvalidOperationException){}}
  void SearchChanged(){if(searchCancel!=null)searchCancel.Cancel();searchGeneration++;searchDelay.Stop();searchDelay.Start();}
  ListViewItem MakeRow(Entry e){var row=new ListViewItem(e.Name){Tag=e,ForeColor=e.Folder?Color.FromArgb(137,190,255):Color.FromArgb(232,235,241)};row.SubItems.Add(e.Kind);row.SubItems.Add(e.Folder?"":SizeText(e.Size));row.SubItems.Add(e.Modified.ToString("yyyy-MM-dd HH:mm"));row.SubItems.Add(e.Attributes.ToString());row.SubItems.Add(System.IO.Path.GetDirectoryName(e.Path));row.ToolTipText=e.Name+"\n"+e.Path;return row;}
  async void Render(){
   if(list==null||IsDisposed)return;if(searchCancel!=null)searchCancel.Cancel();int version=++searchGeneration;
   string query=filter.Text;bool useRegex=regex.Checked;Regex expression=null;
   try{if(useRegex&&query.Length>0)expression=new Regex(query,RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50));}
   catch(ArgumentException ex){visible=new List<Entry>();list.VirtualListSize=0;filter.BackColor=Color.MistyRose;filter.ForeColor=Color.Black;status.Text="Invalid regex: "+ex.Message;return;}
   filter.BackColor=Color.FromArgb(42,46,55);filter.ForeColor=Color.White;if(scanning){status.Text="Reading folders... Search will apply when ready.";return;}
   var cancellation=new CancellationTokenSource();searchCancel=cancellation;var token=cancellation.Token;var snapshot=entries;var order=new EntrySort{Column=sorter.Column,Descending=sorter.Descending};
   status.Text="Searching "+snapshot.Count.ToString("N0")+" names...";
   try{
    var found=await Task.Run(()=>{var output=new List<Entry>();var clock=System.Diagnostics.Stopwatch.StartNew();foreach(var item in snapshot){token.ThrowIfCancellationRequested();if(useRegex&&query.Length>0&&clock.ElapsedMilliseconds>2000)throw new TimeoutException("Regex search took too long. Simplify the expression.");if(query.Length==0||(expression!=null?expression.IsMatch(item.Name):item.Name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0))output.Add(item);}token.ThrowIfCancellationRequested();output.Sort((x,y)=>order.Compare(x,y));return output;},token);
    if(IsDisposed||version!=searchGeneration)return;list.BeginUpdate();try{list.SelectedIndices.Clear();list.VirtualListSize=0;visible=found;list.VirtualListSize=visible.Count;list.Invalidate();if(fit)FitColumns();}finally{list.EndUpdate();}
    status.Text=visible.Count.ToString("N0")+" / "+entries.Count.ToString("N0")+" items"+(subfolders.Checked?" • Including subfolders":"")+(skipped>0?" • "+skipped+" inaccessible items/folders skipped":"");
   }catch(OperationCanceledException){}catch(RegexMatchTimeoutException){if(!IsDisposed&&version==searchGeneration){ClearResults();status.Text="Regex timed out. Simplify the expression.";}}catch(TimeoutException ex){if(!IsDisposed&&version==searchGeneration){ClearResults();status.Text=ex.Message;}}catch(Exception ex){if(!IsDisposed&&version==searchGeneration)status.Text=ex.Message;}
   finally{if(searchCancel==cancellation)searchCancel=null;cancellation.Dispose();}
  }
  void ClearResults(){list.VirtualListSize=0;visible=new List<Entry>();}
  static string SizeText(long n){if(n<1024)return n+" B";if(n<1048576)return (n/1024.0).ToString("N1")+" KB";if(n<1073741824)return(n/1048576.0).ToString("N1")+" MB";return(n/1073741824.0).ToString("N2")+" GB";}
  string[] Selected(){return list.SelectedIndices.Cast<int>().Where(i=>i<visible.Count).Select(i=>visible[i].Path).ToArray();}
  void OpenSelected(){try{if(list.SelectedIndices.Count!=1)return;var e=visible[list.SelectedIndices[0]];if(e.Folder)Navigate(e.Path);else System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Path){UseShellExecute=true});}catch(Exception e){Program.Error(e);}}
  void StartDrag(){var paths=Selected();if(paths.Length==0)return;Program.Dragging=true;try{list.DoDragDrop(new DataObject(DataFormats.FileDrop,paths),DragDropEffects.Copy|DragDropEffects.Move);}finally{Program.Dragging=false;RefreshFiles();}}
  DragDropEffects Effect(DragEventArgs e){if(!e.Data.GetDataPresent(DataFormats.FileDrop))return DragDropEffects.None;var desired=(e.KeyState&4)!=0?DragDropEffects.Move:DragDropEffects.Copy;return (e.AllowedEffect&desired)!=0?desired:DragDropEffects.None;}
  string DestinationAt(DragEventArgs e){var pt=list.PointToClient(new Point(e.X,e.Y));var hit=list.GetItemAt(pt.X,pt.Y);return hit!=null&&hit.Tag is Entry&&((Entry)hit.Tag).Folder?((Entry)hit.Tag).Path:PathNow;}
  internal static bool ValidDestination(string[] sources,string destination){try{string dest=System.IO.Path.GetFullPath(destination).TrimEnd(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar);foreach(var src0 in sources){string src=System.IO.Path.GetFullPath(src0).TrimEnd(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar);string parent=System.IO.Path.GetDirectoryName(src);if(String.Equals(src,dest,StringComparison.OrdinalIgnoreCase)||String.Equals(parent==null?null:parent.TrimEnd(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar),dest,StringComparison.OrdinalIgnoreCase))return false;if(Directory.Exists(src)&&dest.StartsWith(src+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return false;}return sources.Length>0;}catch{return false;}}
  void HandleDragOver(object sender,DragEventArgs e){var effect=Effect(e);if(effect!=DragDropEffects.None){var paths=e.Data.GetData(DataFormats.FileDrop) as string[];if(paths==null||!ValidDestination(paths,DestinationAt(e)))effect=DragDropEffects.None;}e.Effect=effect;}
  async void Drop(object sender,DragEventArgs e){var effect=Effect(e);var paths=e.Data.GetData(DataFormats.FileDrop) as string[];string destination=DestinationAt(e);if(effect==DragDropEffects.None||paths==null||!ValidDestination(paths,destination))return;await Transfer(paths,destination,effect==DragDropEffects.Move);}
  async Task Transfer(string[] sources,string destination,bool move){if(Program.Busy)return;if(!ValidDestination(sources,destination)){status.Text="Choose a different destination folder.";return;}if(move&&MessageBox.Show("Move "+sources.Length+" item(s) to:\n"+destination+"?","Move files",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;Program.Busy=true;try{status.Text=move?"Moving...":"Copying...";await RunSta(()=>FileOps.Execute(sources,destination,move?1u:2u));}catch(OperationCanceledException){}catch(Exception ex){Program.Error(ex);}finally{Program.Busy=false;RefreshFiles();}}
  static Task RunSta(Action action){var completion=new TaskCompletionSource<bool>();var thread=new System.Threading.Thread(()=>{try{action();completion.SetResult(true);}catch(Exception e){completion.SetException(e);}});thread.SetApartmentState(System.Threading.ApartmentState.STA);thread.IsBackground=true;thread.Start();return completion.Task;}
  void Copy(){var paths=Selected();if(paths.Length==0)return;var data=new System.Collections.Specialized.StringCollection();data.AddRange(paths);Clipboard.SetFileDropList(data);}
  async void Paste(){if(Clipboard.ContainsFileDropList())await Transfer(Clipboard.GetFileDropList().Cast<string>().ToArray(),PathNow,false);}
  string Prompt(string title,string value){using(var f=new Form{Text=title,Width=420,Height=145,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){var t=new TextBox{Text=value,Left=12,Top=12,Width=380};var ok=new Button{Text="OK",DialogResult=DialogResult.OK,Left=220,Top=48};var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,Left=305,Top=48};f.Controls.AddRange(new Control[]{t,ok,cancel});f.AcceptButton=ok;f.CancelButton=cancel;return f.ShowDialog(FindForm())==DialogResult.OK?t.Text:null;}}
  string NamePrompt(string title,string value){var n=Prompt(title,value);if(n==null)return null;if(String.IsNullOrWhiteSpace(n)||n.IndexOfAny(System.IO.Path.GetInvalidFileNameChars())>=0||n=="."||n==".."||n.EndsWith(".")||n.EndsWith(" "))throw new IOException("Enter a valid file or folder name.");return n;}
  void Rename(){try{var p=Selected();if(p.Length!=1)return;string name=NamePrompt("Rename",System.IO.Path.GetFileName(p[0]));if(name==null)return;string target=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(p[0]),name);if(Directory.Exists(p[0]))Directory.Move(p[0],target);else File.Move(p[0],target);RefreshFiles();}catch(Exception e){Program.Error(e);}}
  void NewFolder(){try{string n=NamePrompt("New folder","New folder");if(n!=null){string p=System.IO.Path.Combine(PathNow,n);if(Directory.Exists(p)||File.Exists(p))throw new IOException("That name already exists.");Directory.CreateDirectory(p);RefreshFiles();}}catch(Exception e){Program.Error(e);}}
  async void Recycle(){var paths=Selected();if(paths.Length==0||Program.Busy)return;if(MessageBox.Show("Send "+paths.Length+" selected item(s) to the Recycle Bin?","Recycle",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;Program.Busy=true;try{await RunSta(()=>FileOps.Execute(paths,null,3));}catch(OperationCanceledException){}catch(Exception e){Program.Error(e);}finally{Program.Busy=false;RefreshFiles();}}
  void KeysDown(object sender,KeyEventArgs e){if(e.Control&&e.KeyCode==Keys.F){filter.Focus();filter.SelectAll();e.SuppressKeyPress=true;}else if(e.Control&&e.KeyCode==Keys.C){Copy();e.SuppressKeyPress=true;}else if(e.Control&&e.KeyCode==Keys.V){Paste();e.SuppressKeyPress=true;}else if(e.Control&&e.KeyCode==Keys.A){for(int i=0;i<visible.Count;i++)list.SelectedIndices.Add(i);e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.F5){RefreshFiles();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.F2){Rename();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Delete){Recycle();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Enter){OpenSelected();e.SuppressKeyPress=true;}else if(e.Alt&&e.KeyCode==Keys.Up){var p=Directory.GetParent(PathNow);if(p!=null)Navigate(p.FullName);e.SuppressKeyPress=true;}}
 }
}
