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
  public int Width=700, LeftWidth=0, RightWidth=0, SpotPercent=50, SpotHeight=100, Delay=250, Monitor=0;
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
   Log("Starting Hedge 0.4.1");
   try{
    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
    Application.ThreadException+=(sender,args)=>Report(args.Exception);
    AppDomain.CurrentDomain.UnhandledException+=(sender,args)=>{var e=args.ExceptionObject as Exception;if(e!=null)Report(e);else Log("Unhandled error: "+args.ExceptionObject);};
    bool created;using(var mutex=new System.Threading.Mutex(true,"Local\\Hedge.v1",out created)){
     if(!created){Log("Another instance is running.");MessageBox.Show("Hedge is already running. Use the blue screen-edge strip or the Hedge icon in the system tray to open a drawer.","Hedge",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
     DarkWindows.Enable();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Config=Settings.Load();
     if(Config.Favorites==null)Config.Favorites=new List<string>();if(Config.LeftA==null)Config.LeftA=new List<string>();if(Config.LeftB==null)Config.LeftB=new List<string>();if(Config.RightA==null)Config.RightA=new List<string>();if(Config.RightB==null)Config.RightB=new List<string>();
     Log("Settings loaded; creating drawers.");var host=new Host();Log("Drawers created; showing the left drawer.");host.ShowInitial();if(Environment.GetCommandLineArgs().Contains("--smoke-test")){var testTimer=new Timer{Interval=800};testTimer.Tick+=(ss,ee)=>{testTimer.Stop();try{host.SmokeTest();Log("UI_SMOKE_OK");}catch(Exception e){Log("UI_SMOKE_ERROR: "+e);}finally{testTimer.Dispose();host.ExitThread();}};testTimer.Start();}Application.Run(host);Log("Exit completed.");
    }
   }catch(Exception error){Report(error);}
  }
  public static Button Icon(string text,Action action,string tip){var b=new Button{Text=text,Width=32,Height=29,AutoSize=false,Margin=new Padding(0),Font=new Font("Segoe UI Symbol",13,FontStyle.Regular),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(32,35,41),ForeColor=Color.FromArgb(220,225,234),Cursor=Cursors.Hand};b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=Color.FromArgb(55,63,76);b.Click+=(s,e)=>action();var tt=new ToolTip();tt.SetToolTip(b,tip);b.Disposed+=(s,e)=>tt.Dispose();return b;}
  public static ContextMenuStrip DarkMenu(){var menu=new ContextMenuStrip{BackColor=Color.FromArgb(37,40,47),ForeColor=Color.FromArgb(232,235,240),ShowImageMargin=false,Renderer=new ToolStripProfessionalRenderer(new DarkColors())};return menu;}
  public static Button Button(string text,Action action){var b=new Button{Text=text,AutoSize=true,Height=29,FlatStyle=FlatStyle.System};b.Click+=(s,e)=>action();return b;}
  public static void Theme(Control c){
   c.BackColor=Color.FromArgb(30,32,38);c.ForeColor=Color.FromArgb(230,233,240);
   var button=c as Button;if(button!=null){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=0;button.FlatAppearance.BorderColor=Color.FromArgb(62,67,79);button.FlatAppearance.MouseOverBackColor=Color.FromArgb(57,69,89);button.BackColor=Color.FromArgb(42,46,55);}
   var tb=c as TextBox;if(tb!=null){tb.BackColor=Color.FromArgb(42,46,55);tb.ForeColor=Color.White;tb.BorderStyle=BorderStyle.FixedSingle;}
   var lv=c as ListView;if(lv!=null){lv.BackColor=Color.FromArgb(27,29,34);lv.ForeColor=Color.FromArgb(232,235,241);lv.GridLines=false;}
   var lb=c as ListBox;if(lb!=null){lb.BackColor=Color.FromArgb(35,38,45);lb.ForeColor=Color.FromArgb(230,233,240);lb.BorderStyle=BorderStyle.None;}
   foreach(Control child in c.Controls)Theme(child);
  }
  public static void Error(Exception e){MessageBox.Show(e.Message,"Hedge",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
 }
 class DarkColors:ProfessionalColorTable {public override Color MenuItemSelected {get{return Color.FromArgb(58,66,79);}}public override Color MenuItemBorder {get{return Color.FromArgb(58,66,79);}}public override Color ToolStripDropDownBackground {get{return Color.FromArgb(37,40,47);}}public override Color ImageMarginGradientBegin {get{return ToolStripDropDownBackground;}}public override Color ImageMarginGradientMiddle {get{return ToolStripDropDownBackground;}}public override Color ImageMarginGradientEnd {get{return ToolStripDropDownBackground;}}}
 class Host:ApplicationContext {
  Drawer left,right;NotifyIcon tray;bool exiting;
  public Host(){left=new Drawer(true);right=new Drawer(false);tray=new NotifyIcon{Text="Hedge • left and right file drawers",Icon=SystemIcons.Application,Visible=true};var menu=new ContextMenuStrip();menu.Items.Add("Open left",null,(s,e)=>left.Reveal());menu.Items.Add("Open right",null,(s,e)=>right.Reveal());menu.Items.Add("Settings",null,(s,e)=>EditSettings());menu.Items.Add("Exit",null,(s,e)=>ExitThread());tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>left.Reveal();left.Show();right.Show();left.Collapse();right.Collapse();}
  public void ShowInitial(){left.Reveal();}
  public void SmokeTest(){left.SmokeTest();right.SmokeEdge();}
  void EditSettings(){using(var f=new Form{Text="Hedge settings",Size=new Size(450,430),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){var table=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=8};table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));f.Controls.Add(table);var c=Program.Config;
  var width=Number(table,"Drawer width (pixels)",c.Width,420,1400);var pos=Number(table,"Hotspot position (% down edge)",c.SpotPercent,5,95);var height=Number(table,"Hotspot height (pixels)",c.SpotHeight,30,600);var delay=Number(table,"Hover delay (milliseconds)",c.Delay,0,2000);var monitor=Number(table,"Monitor (1 = first display)",c.Monitor+1,1,Screen.AllScreens.Length);var hidden=new CheckBox{Text="Show hidden files",Checked=c.Hidden,AutoSize=true};table.Controls.Add(hidden);table.SetColumnSpan(hidden,2);table.Controls.Add(new Label{Text="Edge strips stay visible. Drag files onto a strip to open its drawer.",AutoSize=true});table.SetColumnSpan(table.Controls[table.Controls.Count-1],2);
  table.Controls.Add(Program.Button("Save",()=>{c.Width=(int)width.Value;c.SpotPercent=(int)pos.Value;c.SpotHeight=(int)height.Value;c.Delay=(int)delay.Value;c.Monitor=(int)monitor.Value-1;c.Hidden=hidden.Checked;left.Position();right.Position();left.RefreshAll();right.RefreshAll();Save();f.Close();}));f.ShowDialog();}}
  NumericUpDown Number(TableLayoutPanel t,string label,int value,int min,int max){t.Controls.Add(new Label{Text=label,AutoSize=true,Anchor=AnchorStyles.Left});var n=new NumericUpDown{Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),Dock=DockStyle.Fill};t.Controls.Add(n);return n;}
  void Save(){try{left.Store();right.Store();Program.Config.Save();}catch(Exception e){Program.Error(e);}}
  protected override void ExitThreadCore(){if(exiting)return;if(Program.Busy||Program.Dragging){MessageBox.Show("Wait for the current file operation to finish before exiting.","Hedge");return;}exiting=true;Save();tray.Visible=false;tray.Dispose();left.Shutdown();right.Shutdown();base.ExitThreadCore();}
 }
 class Drawer:Form {
  bool left,expanded,closing,pinned;int drawerWidth;DateTime hover=DateTime.MinValue,away=DateTime.MinValue;
  SplitContainer split;TabPane a,b;Form strip;Timer timer;Button pin;public Drawer(bool isLeft){left=isLeft;drawerWidth=(isLeft?Program.Config.LeftWidth:Program.Config.RightWidth);if(drawerWidth<320)drawerWidth=Program.Config.Width;Text="Hedge • "+(left?"Left":"Right");FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.FromArgb(228,232,238);Padding=new Padding(2);Font=new Font("Segoe UI",9);
   var header=new FlowLayoutPanel{Dock=DockStyle.Top,Height=32,Padding=new Padding(2,1,0,0),WrapContents=false};pin=Program.Icon("⌖",()=>{pinned=!pinned;pin.ForeColor=pinned?Color.FromArgb(130,194,255):Color.FromArgb(220,225,234);},"Pin drawer open");header.Controls.Add(pin);header.Controls.Add(Program.Icon("◫",()=>{split.Panel2Collapsed=!split.Panel2Collapsed;},"One or two views"));header.Controls.Add(Program.Icon("⇄",()=>{split.Orientation=split.Orientation==Orientation.Horizontal?Orientation.Vertical:Orientation.Horizontal;},"Stack views or place side by side"));header.Controls.Add(Program.Icon(left?"⟨":"⟩",()=>Collapse(),"Collapse drawer"));
   split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,Size=new Size(700,650),SplitterWidth=6,Panel1MinSize=150,Panel2MinSize=150,SplitterDistance=320};a=new TabPane(left?Program.Config.LeftA:Program.Config.RightA);b=new TabPane(left?Program.Config.LeftB:Program.Config.RightB);split.Panel1.Controls.Add(a);split.Panel2.Controls.Add(b);split.Panel2Collapsed=!(left?Program.Config.LeftSplit:Program.Config.RightSplit);Controls.Add(split);Controls.Add(header);var edge=new Panel{Dock=left?DockStyle.Right:DockStyle.Left,Width=7,Cursor=Cursors.SizeWE,BackColor=Color.FromArgb(43,47,56)};Controls.Add(edge);edge.BringToFront();bool resizing=false;edge.MouseDown+=(ss,ee)=>{if(ee.Button==MouseButtons.Left){resizing=true;edge.Capture=true;}};edge.MouseMove+=(ss,ee)=>{if(!resizing)return;var r=Screen.FromControl(this).WorkingArea;int desired=left?Cursor.Position.X-r.Left:r.Right-Cursor.Position.X;drawerWidth=Math.Max(360,Math.Min(r.Width,desired));Position();};edge.MouseUp+=(ss,ee)=>{if(resizing){resizing=false;edge.Capture=false;if(left)Program.Config.LeftWidth=drawerWidth;else Program.Config.RightWidth=drawerWidth;}};Program.Theme(this);
   strip=new EdgeStrip{FormBorderStyle=FormBorderStyle.None,ShowInTaskbar=false,TopMost=true,BackColor=Color.FromArgb(82,96,115),Opacity=.6,AllowDrop=true};strip.Cursor=Cursors.Hand;strip.MouseEnter+=(s,e)=>hover=DateTime.UtcNow;strip.MouseLeave+=(s,e)=>hover=DateTime.MinValue;strip.Click+=(s,e)=>Reveal();strip.DragEnter+=(s,e)=>{if(e.Data.GetDataPresent(DataFormats.FileDrop)){e.Effect=DragDropEffects.Copy;Reveal();}};
   timer=new Timer{Interval=40};timer.Tick+=(s,e)=>TickEdge();timer.Start();SystemEventsHook();Position();strip.Show();FormClosing+=(s,e)=>{if(!closing){e.Cancel=true;Collapse();}};
  }
  void SystemEventsHook(){Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplayChanged;}
  void DisplayChanged(object s,EventArgs e){if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)Position);}
  public void Position(){var screens=Screen.AllScreens;var screen=screens[Math.Max(0,Math.Min(Program.Config.Monitor,screens.Length-1))];var r=screen.WorkingArea;int w=Math.Min(Math.Max(360,drawerWidth),r.Width);Bounds=new Rectangle(left?r.Left:r.Right-w,r.Top,w,r.Height);int h=Math.Min(Program.Config.SpotHeight,r.Height);int y=r.Top+(int)((r.Height-h)*Program.Config.SpotPercent/100.0);((EdgeStrip)strip).SetEdgeBounds(new Rectangle(left?r.Left:r.Right-4,y,4,h));}
  public void SmokeEdge(){((EdgeStrip)strip).VerifyEdge();}
  public void SmokeTest(){a.SmokeTest();int old=drawerWidth;SetWidth(old+40);Position();if(drawerWidth!=old+40)throw new Exception("Width did not change");SetWidth(old);Position();}
  public void SetWidth(int width){drawerWidth=width;if(left)Program.Config.LeftWidth=width;else Program.Config.RightWidth=width;}
  void TickEdge(){if(!expanded){if(hover!=DateTime.MinValue&&(DateTime.UtcNow-hover).TotalMilliseconds>=Program.Config.Delay)Reveal();return;}if(pinned||Program.Dragging||Program.Busy||ContainsFocus||Bounds.Contains(Cursor.Position)){away=DateTime.MinValue;return;}if(away==DateTime.MinValue)away=DateTime.UtcNow;if((DateTime.UtcNow-away).TotalMilliseconds>700)Collapse();}
  public void Reveal(){if(expanded)return;expanded=true;away=DateTime.MinValue;Position();Show();BringToFront();strip.Hide();}
  public void Collapse(){expanded=false;Hide();hover=DateTime.MinValue;strip.Show();Position();}
  public void RefreshAll(){a.RefreshActive();b.RefreshActive();}
  public void Store(){if(left){Program.Config.LeftA=a.Paths();Program.Config.LeftB=b.Paths();Program.Config.LeftSplit=!split.Panel2Collapsed;}else{Program.Config.RightA=a.Paths();Program.Config.RightB=b.Paths();Program.Config.RightSplit=!split.Panel2Collapsed;}}
  public void Shutdown(){closing=true;Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplayChanged;timer.Stop();timer.Dispose();strip.Close();strip.Dispose();Close();Dispose();}
 }
 class EdgeStrip:Form {
  Rectangle edgeBounds;
  [StructLayout(LayoutKind.Sequential)]struct WinRect{public int left,top,right,bottom;}
  [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr hwnd,out WinRect rect);
  public void VerifyEdge(){if(Environment.OSVersion.Platform!=PlatformID.Win32NT)return;WinRect r;if(!GetWindowRect(Handle,out r)||r.right-r.left>6)throw new Exception("Collapsed edge strip is wider than expected: "+(r.right-r.left));}
  [DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
  public void SetEdgeBounds(Rectangle bounds){edgeBounds=bounds;if(Environment.OSVersion.Platform==PlatformID.Win32NT&&IsHandleCreated){SetWindowPos(Handle,IntPtr.Zero,bounds.X,bounds.Y,bounds.Width,bounds.Height,0x0014);}else Bounds=bounds;}
  protected override void OnShown(EventArgs e){base.OnShown(e);SetEdgeBounds(edgeBounds);}
 }
 class TabStrip:Control {
  public event Action<int> SelectTab;public event Action<int> CloseTab;
  public readonly List<string> Titles=new List<string>();public int Active;int offset,hover=-1;bool closeHover;const int ItemWidth=144,ArrowWidth=22;readonly ToolTip tip=new ToolTip();
  public TabStrip(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);Dock=DockStyle.Fill;BackColor=Color.FromArgb(26,28,33);ForeColor=Color.FromArgb(230,233,240);Cursor=Cursors.Default;}
  bool Overflow {get{return Titles.Count*ItemWidth>Width;}}int ViewWidth {get{return Math.Max(0,Width-(Overflow?2*ArrowWidth:0));}}int MaxOffset {get{return Math.Max(0,Titles.Count*ItemWidth-ViewWidth);}}int LeftInset {get{return Overflow?ArrowWidth:0;}}
  public void UpdateTabs(IEnumerable<string> titles,int selected,bool ensureVisible){Titles.Clear();Titles.AddRange(titles);Active=selected;offset=Math.Max(0,Math.Min(offset,MaxOffset));if(ensureVisible&&Active>=0){int x=Active*ItemWidth;if(x<offset)offset=x;else if(x+ItemWidth>offset+ViewWidth)offset=x+ItemWidth-ViewWidth;offset=Math.Max(0,Math.Min(offset,MaxOffset));}Invalidate();}
  public bool ScrollBy(int delta){int before=offset;offset=Math.Max(0,Math.Min(MaxOffset,offset+delta));Invalidate();return offset!=before;}
  protected override void OnResize(EventArgs e){base.OnResize(e);offset=Math.Max(0,Math.Min(offset,MaxOffset));Invalidate();}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.Clear(BackColor);var g=e.Graphics;var clip=g.Save();g.SetClip(new Rectangle(LeftInset,0,ViewWidth,Height));for(int i=0;i<Titles.Count;i++){int x=LeftInset+i*ItemWidth-offset;var rect=new Rectangle(x,2,ItemWidth-2,Height-3);if(rect.Right<LeftInset||rect.Left>LeftInset+ViewWidth)continue;using(var b=new SolidBrush(i==Active?Color.FromArgb(48,53,64):i==hover?Color.FromArgb(39,43,51):Color.FromArgb(30,32,38)))g.FillRectangle(b,rect);var textRect=new Rectangle(x+10,0,ItemWidth-42,Height);TextRenderer.DrawText(g,Titles[i],Font,textRect,ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);if(Titles.Count>1){var xRect=new Rectangle(x+ItemWidth-30,3,23,Height-6);if(hover==i&&closeHover){using(var b=new SolidBrush(Color.FromArgb(75,81,94)))g.FillEllipse(b,xRect);}using(var f=new Font("Segoe UI Symbol",11)){TextRenderer.DrawText(g,"×",f,xRect,Color.FromArgb(224,228,235),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);}}}g.Restore(clip);if(Overflow){using(var b=new SolidBrush(BackColor)){g.FillRectangle(b,0,0,ArrowWidth,Height);g.FillRectangle(b,Width-ArrowWidth,0,ArrowWidth,Height);}using(var f=new Font("Segoe UI Symbol",13)){TextRenderer.DrawText(g,"‹",f,new Rectangle(0,0,ArrowWidth,Height),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);TextRenderer.DrawText(g,"›",f,new Rectangle(Width-ArrowWidth,0,ArrowWidth,Height),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);}}}
  void Hit(Point p,out int index,out bool close){index=-1;close=false;if(p.X<LeftInset||p.X>=LeftInset+ViewWidth)return;int local=p.X-LeftInset+offset;index=local/ItemWidth;if(index<0||index>=Titles.Count){index=-1;return;}close=Titles.Count>1&&(local%ItemWidth)>=ItemWidth-32;}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int i;bool x;Hit(e.Location,out i,out x);if(i!=hover||x!=closeHover){hover=i;closeHover=x;tip.SetToolTip(this,x?"Close tab":i>=0?Titles[i]:Overflow?"Scroll tabs":"");Invalidate();}}
  protected override void OnMouseLeave(EventArgs e){hover=-1;closeHover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;if(Overflow&&e.X<ArrowWidth){ScrollBy(-ItemWidth);return;}if(Overflow&&e.X>=Width-ArrowWidth){ScrollBy(ItemWidth);return;}int i;bool x;Hit(e.Location,out i,out x);if(i<0)return;if(x){if(CloseTab!=null)CloseTab(i);}else if(SelectTab!=null)SelectTab(i);}
  protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
 }
 class TabPane:UserControl {
  readonly List<FileView> views=new List<FileView>();FileView current;Panel content;TabStrip tabStrip;ContextMenuStrip drives;ToolTip tips=new ToolTip();
  public TabPane(List<string> paths){Dock=DockStyle.Fill;BackColor=Color.FromArgb(30,32,38);
   var bar=new Panel{Dock=DockStyle.Top,Height=32,BackColor=Color.FromArgb(26,28,33)};
   var actions=new FlowLayoutPanel{Dock=DockStyle.Right,Width=128,Height=32,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,BackColor=bar.BackColor};
   Button add=Program.Icon("+",()=>Add(current==null?Environment.GetFolderPath(Environment.SpecialFolder.UserProfile):current.PathNow),"New tab");Button drive=Program.Icon("▣",()=>ToggleDrives(),"Drives");Button home=Program.Icon("⌂",()=>{if(current!=null)current.Navigate(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));},"Home");Button places=Program.Icon("☰",()=>{if(current!=null)current.TogglePlaces();},"Toggle shortcuts");actions.Controls.Add(add);actions.Controls.Add(home);actions.Controls.Add(drive);actions.Controls.Add(places);
   tabStrip=new TabStrip();tabStrip.SelectTab+=i=>{if(i>=0&&i<views.Count)SelectView(views[i]);};tabStrip.CloseTab+=i=>{if(i>=0&&i<views.Count)CloseView(views[i]);};bar.Controls.Add(tabStrip);bar.Controls.Add(actions);
   drives=Program.DarkMenu();
   drives.Opening+=(ss,ee)=>{drives.Items.Clear();foreach(var d in DriveInfo.GetDrives()){string path=d.Name;drives.Items.Add(DriveLabel(d),null,(s2,e2)=>{if(current!=null)current.Navigate(path);});}ee.Cancel=drives.Items.Count==0;};
   content=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(27,29,34)};Controls.Add(content);Controls.Add(bar);
   if(paths==null||paths.Count==0)Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));else foreach(var path in paths)Add(Directory.Exists(path)?path:Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
  }
  public void SmokeTest(){int before=views.Count;ToggleDrives();if(!drives.Visible)throw new Exception("Drive menu did not open");ToggleDrives();if(drives.Visible)throw new Exception("Drive menu did not close");ToggleDrives();drives.Close();Add(current.PathNow);if(views.Count!=before+1)throw new Exception("Tab did not open");CloseView(current);if(views.Count!=before)throw new Exception("Tab did not close");if(current==null)throw new Exception("No active view");for(int i=0;i<12;i++)Add(current.PathNow);if(tabStrip.Titles.Count!=before+12)throw new Exception("Missing overflow tabs");tabStrip.ScrollBy(-144);tabStrip.ScrollBy(144);while(views.Count>before)CloseView(views[views.Count-1]);}
  void ToggleDrives(){if(drives.Visible){drives.Close();return;}drives.Show(this,Math.Max(0,Width-95),32);}
  internal static string DriveLabel(DriveInfo d){string label="";try{label=d.VolumeLabel??"";}catch{}string remote=DarkWindows.NetworkName(d.Name);bool google=label.IndexOf("google",StringComparison.OrdinalIgnoreCase)>=0||remote.IndexOf("google",StringComparison.OrdinalIgnoreCase)>=0;if(!google&&d.Name.StartsWith("Z:",StringComparison.OrdinalIgnoreCase)){try{google=System.Diagnostics.Process.GetProcessesByName("GoogleDriveFS").Length>0;}catch{}}return (google?"Google Drive":String.IsNullOrWhiteSpace(label)?"Drive":label)+" ("+d.Name.TrimEnd('\\')+")";}
  void Add(string path){var view=new FileView(path);view.Visible=false;view.PathChanged+=RebuildTabs;views.Add(view);content.Controls.Add(view);SelectView(view);}
  void SelectView(FileView view){if(current!=null)current.Visible=false;current=view;current.Visible=true;current.BringToFront();RebuildTabs();current.RefreshFiles();}
  void CloseView(FileView view){if(views.Count==1)return;int i=views.IndexOf(view);bool wasCurrent=current==view;views.Remove(view);content.Controls.Remove(view);view.Dispose();if(wasCurrent)SelectView(views[Math.Min(i,views.Count-1)]);else RebuildTabs();}
  void RebuildTabs(){tabStrip.UpdateTabs(views.Select(v=>Short(v.PathNow)),views.IndexOf(current),true);}
  string Short(string path){string name=System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar));return name.Length==0?path:name;}
  public List<string> Paths(){return views.Select(v=>v.PathNow).ToList();}
  public void RefreshActive(){if(current!=null)current.RefreshFiles();}
  protected override void Dispose(bool disposing){if(disposing){drives.Dispose();tips.Dispose();}base.Dispose(disposing);}
 }
 class Entry {public string Path,Name,Kind;public bool Folder;public long Size;public DateTime Modified;public FileAttributes Attributes;}
 class EntrySort:IComparer {public int Column;public bool Descending;public int Compare(object x,object y){var a=(Entry)x;var b=(Entry)y;if(a.Folder!=b.Folder)return a.Folder?-1:1;int n=Column==2?a.Size.CompareTo(b.Size):Column==3?a.Modified.CompareTo(b.Modified):StringComparer.CurrentCultureIgnoreCase.Compare(Column==1?a.Kind:Column==4?a.Attributes.ToString():Column==5?System.IO.Path.GetDirectoryName(a.Path):a.Name,Column==1?b.Kind:Column==4?b.Attributes.ToString():Column==5?System.IO.Path.GetDirectoryName(b.Path):b.Name);return Descending?-n:n;}}
 static class DarkWindows {
  [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)]static extern int SetWindowTheme(IntPtr hwnd,string subApp,string subId);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string name);
  [DllImport("kernel32.dll",CharSet=CharSet.Ansi)]static extern IntPtr GetProcAddress(IntPtr module,IntPtr ordinal);
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct OSVersion {public int size,major,minor,build,platform;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string csd;}
  [DllImport("ntdll.dll")]static extern int RtlGetVersion(ref OSVersion version);
  [DllImport("mpr.dll",CharSet=CharSet.Unicode)]static extern int WNetGetConnection(string local,System.Text.StringBuilder remote,ref int length);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int PreferredMode(int mode);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate bool AllowWindow(IntPtr hwnd,bool allow);
  static AllowWindow allow;
  public static void Enable(){try{if(Environment.OSVersion.Platform!=PlatformID.Win32NT)return;var os=new OSVersion();os.size=Marshal.SizeOf(typeof(OSVersion));if(RtlGetVersion(ref os)!=0||os.build<18362)return;IntPtr module=GetModuleHandle("uxtheme.dll");if(module==IntPtr.Zero)return;IntPtr preferred=GetProcAddress(module,(IntPtr)135),window=GetProcAddress(module,(IntPtr)133);if(preferred!=IntPtr.Zero)((PreferredMode)Marshal.GetDelegateForFunctionPointer(preferred,typeof(PreferredMode)))(1);if(window!=IntPtr.Zero)allow=(AllowWindow)Marshal.GetDelegateForFunctionPointer(window,typeof(AllowWindow));}catch{}}
  public static void Apply(IntPtr h){try{if(allow!=null)allow(h,true);SetWindowTheme(h,"DarkMode_Explorer",null);}catch{}}
  public static string NetworkName(string drive){try{var buffer=new System.Text.StringBuilder(512);int size=buffer.Capacity;return WNetGetConnection(drive.TrimEnd('\\'),buffer,ref size)==0?buffer.ToString():"";}catch{return "";}}
 }

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
  public string PathNow;public event Action PathChanged;TextBox address,filter;CheckBox regex,subfolders;ListView list;Label status;Stack<string> back=new Stack<string>(),forward=new Stack<string>();EntrySort sorter=new EntrySort();List<Entry> entries=new List<Entry>();int generation,searchGeneration;bool scanning;int skipped;ListBox places;Panel placesPanel;List<Entry> visible=new List<Entry>();CancellationTokenSource scanCancel,searchCancel;FileSystemWatcher watcher;Timer debounce,searchDelay;
  public FileView(string path){Dock=DockStyle.Fill;PathNow=path;var nav=new TableLayoutPanel{Dock=DockStyle.Top,Height=34,ColumnCount=5,Padding=new Padding(2,2,2,0)};for(int i=0;i<4;i++)nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,34));nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));nav.Controls.Add(Program.Icon("‹",()=>History(back,forward),"Back"));nav.Controls.Add(Program.Icon("›",()=>History(forward,back),"Forward"));nav.Controls.Add(Program.Icon("↑",()=>{var p=Directory.GetParent(PathNow);if(p!=null)Navigate(p.FullName);},"Parent folder"));nav.Controls.Add(Program.Icon("⌄",()=>ShowHierarchy(),"Folder hierarchy"));address=new TextBox{Dock=DockStyle.Fill,Text=path,Margin=new Padding(3,4,3,2),BorderStyle=BorderStyle.FixedSingle};address.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){Navigate(Environment.ExpandEnvironmentVariables(address.Text));e.SuppressKeyPress=true;}};nav.Controls.Add(address);
   filter=new TextBox{Dock=DockStyle.Fill};
   regex=Toggle(".*","Regular expression");subfolders=Toggle("Sub","Include subfolders");
   var searchBar=new TableLayoutPanel{Dock=DockStyle.Top,Height=34,ColumnCount=4,Padding=new Padding(2)};
   searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,38));searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
   searchBar.Controls.Add(new Label{Text="Find",AutoSize=true,Margin=new Padding(0,5,0,0)});searchBar.Controls.Add(filter);searchBar.Controls.Add(regex);searchBar.Controls.Add(subfolders);
   searchDelay=new Timer{Interval=120};searchDelay.Tick+=(s,e)=>{searchDelay.Stop();Render();};filter.TextChanged+=(s,e)=>SearchChanged();regex.CheckedChanged+=(s,e)=>SearchChanged();subfolders.CheckedChanged+=(s,e)=>{searchDelay.Stop();RefreshFiles();};
   list=new ListView{Dock=DockStyle.Fill,ShowItemToolTips=true,View=View.Details,FullRowSelect=true,GridLines=false,BorderStyle=BorderStyle.None,OwnerDraw=true,HideSelection=false,MultiSelect=true,AllowDrop=true};list.Columns.Add("Name",245);list.Columns.Add("Type",95);list.Columns.Add("Size",90,HorizontalAlignment.Right);list.Columns.Add("Modified",145);list.Columns.Add("Attributes",100);list.Columns.Add("Folder",240);list.DrawColumnHeader+=(ss,ee)=>{using(var brush=new SolidBrush(Color.FromArgb(39,42,49)))ee.Graphics.FillRectangle(brush,ee.Bounds);TextRenderer.DrawText(ee.Graphics,ee.Header.Text,list.Font,ee.Bounds,Color.FromArgb(195,202,214),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};list.DrawItem+=(ss,ee)=>{if(list.View!=View.Details)ee.DrawDefault=true;};list.DrawSubItem+=(ss,ee)=>{bool selected=ee.Item.Selected;using(var brush=new SolidBrush(selected?Color.FromArgb(49,65,88):Color.FromArgb(27,29,34)))ee.Graphics.FillRectangle(brush,ee.Bounds);var flags=TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix;if(ee.ColumnIndex==2)flags|=TextFormatFlags.Right;else flags|=TextFormatFlags.Left;var rect=ee.Bounds;rect.Inflate(-5,0);TextRenderer.DrawText(ee.Graphics,ee.SubItem.Text,list.Font,rect,ee.ColumnIndex==0?ee.Item.ForeColor:Color.FromArgb(203,208,218),flags);};list.HandleCreated+=(ss,ee)=>DarkWindows.Apply(list.Handle);list.VirtualMode=true;list.RetrieveVirtualItem+=(s,e)=>{e.Item=e.ItemIndex<visible.Count?MakeRow(visible[e.ItemIndex]):new ListViewItem("");};list.ColumnClick+=(s,e)=>{sorter.Descending=sorter.Column==e.Column&&!sorter.Descending;sorter.Column=e.Column;Render();};list.DoubleClick+=(s,e)=>OpenSelected();list.ItemDrag+=(s,e)=>StartDrag();list.DragEnter+=HandleDragOver;list.DragOver+=HandleDragOver;list.DragDrop+=Drop;list.KeyDown+=KeysDown;
   placesPanel=new Panel{Dock=DockStyle.Left,Width=152,Padding=new Padding(6,4,6,4)};places=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false};places.HandleCreated+=(ss,ee)=>DarkWindows.Apply(places.Handle);placesPanel.Controls.Add(places);places.MouseUp+=(ss,ee)=>{if(ee.Button==MouseButtons.Left){var place=places.SelectedItem as Place;if(place!=null)Navigate(place.Path);}};Place contextPlace=null;places.MouseDown+=(ss,ee)=>{if(ee.Button==MouseButtons.Right){int index=places.IndexFromPoint(ee.Location);contextPlace=index>=0&&index<places.Items.Count?places.Items[index] as Place:null;}};var placeMenu=Program.DarkMenu();placeMenu.Items.Add("Pin current folder",null,(ss,ee)=>PinCurrent());placeMenu.Items.Add("Remove pinned folder",null,(ss,ee)=>{var item=contextPlace;if(item!=null){Program.Config.Favorites.RemoveAll(x=>String.Equals(x,item.Path,StringComparison.OrdinalIgnoreCase));FillPlaces();}});places.ContextMenuStrip=placeMenu;FillPlaces();
   var menu=Program.DarkMenu();menu.Items.Add("Pin folder",null,(s,e)=>PinSelection());menu.Items.Add("Open",null,(s,e)=>OpenSelected());menu.Items.Add("Open in File Explorer",null,(s,e)=>{System.Diagnostics.Process.Start("explorer.exe",PathNow);});menu.Items.Add("Copy",null,(s,e)=>Copy());menu.Items.Add("Paste",null,(s,e)=>Paste());menu.Items.Add("Rename",null,(s,e)=>Rename());menu.Items.Add("New folder",null,(s,e)=>NewFolder());menu.Items.Add("Recycle selected",null,(s,e)=>Recycle());menu.Items.Add("Refresh",null,(s,e)=>RefreshFiles());list.ContextMenuStrip=menu;
   status=new Label{Dock=DockStyle.Bottom,Height=25,Text="Loading...",Padding=new Padding(4)};Controls.Add(list);Controls.Add(placesPanel);Controls.Add(status);Controls.Add(searchBar);Controls.Add(nav);debounce=new Timer{Interval=400};debounce.Tick+=(s,e)=>{debounce.Stop();RefreshFiles();};Load+=(s,e)=>RefreshFiles();Disposed+=(s,e)=>{generation++;searchGeneration++;if(scanCancel!=null)scanCancel.Cancel();if(searchCancel!=null)searchCancel.Cancel();if(watcher!=null)watcher.Dispose();debounce.Dispose();searchDelay.Dispose();};Program.Theme(this);}
  public void TogglePlaces(){placesPanel.Visible=!placesPanel.Visible;}
  void PinCurrent(){PinPath(PathNow);}
  void PinPath(string path){if(!Program.Config.Favorites.Any(x=>String.Equals(x,path,StringComparison.OrdinalIgnoreCase)))Program.Config.Favorites.Add(path);FillPlaces();}
  void PinSelection(){var selected=Selected();PinPath(selected.Length==1&&Directory.Exists(selected[0])?selected[0]:PathNow);}
  void AddPlace(string name,string path){if(!String.IsNullOrEmpty(path)&&Directory.Exists(path))places.Items.Add(new Place{Label=name,Path=path});}
  void FillPlaces(){places.Items.Clear();AddPlace("Home",Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));AddPlace("Desktop",Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));AddPlace("Downloads",KnownPlaces.Downloads());AddPlace("Documents",Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));AddPlace("OneDrive",Environment.GetEnvironmentVariable("OneDrive"));foreach(var item in Program.Config.Favorites.ToArray())AddPlace("★ "+(System.IO.Path.GetFileName(item.TrimEnd(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar))==""?item:System.IO.Path.GetFileName(item.TrimEnd(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar))),item);foreach(var drive in DriveInfo.GetDrives())AddPlace(TabPane.DriveLabel(drive),drive.Name);}
  static CheckBox Toggle(string label,string tooltip){var b=new CheckBox{Appearance=Appearance.Button,Text=label,Width=44,Height=28,AutoSize=false,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleCenter,Margin=new Padding(1,0,1,0),BackColor=Color.FromArgb(42,46,55),ForeColor=Color.FromArgb(230,233,240)};b.FlatAppearance.BorderSize=0;b.FlatAppearance.CheckedBackColor=Color.FromArgb(51,89,133);var tip=new ToolTip();tip.SetToolTip(b,tooltip);b.Disposed+=(s,e)=>tip.Dispose();return b;}
  void ShowHierarchy(){var menu=Program.DarkMenu();string path=PathNow;while(!String.IsNullOrEmpty(path)){string target=path;menu.Items.Add(path,null,(ss,ee)=>Navigate(target));try{var parent=Directory.GetParent(path);path=parent==null?null:parent.FullName;}catch{break;}}menu.Closed+=(ss,ee)=>BeginInvoke((Action)(()=>menu.Dispose()));menu.Show(address,0,address.Height);}
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
    if(IsDisposed||version!=searchGeneration)return;list.BeginUpdate();try{list.SelectedIndices.Clear();list.VirtualListSize=0;visible=found;list.VirtualListSize=visible.Count;list.Invalidate();}finally{list.EndUpdate();}
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
