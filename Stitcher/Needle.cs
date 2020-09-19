using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video.FFMPEG;
using AForge.Video.DirectShow;

namespace Needle
{
    public partial class Needle : Form
    {
        //DATA
        public const string fileFormatVersion = "Needle SDxArrv2.1";
        public const string stitcherVersion = "[INDEV 2.7]";
        //FILM
        public Dictionary<string, string> meta;
        public List<List<string>> film;
        private VideoCaptureDevice vsource;
        private List<List<string>> scenes = new List<List<string>>();
        private Bitmap lfm;
        //REFERENCES
        private List<string> refs = new List<string>(); //Locations
        private List<Tuple<Label, Rectangle>> refcts = new List<Tuple<Label, Rectangle>>(); //Labels
        //INTERTHREAD GLOBALS
        private string photoSaveLocation = "", videoDeviceMoniker = "";
        private int openScene = 0;
        private bool selectImage = false;
        //UI
        private Form launcher;
        private Button framelistnewbutt, scenelistnewbutt;
        private MenuStrip menuStrip1;
        private StatusBar statusBar;
        private StatusBarPanel statusBarDate, statusBarInstruction;
        private List<StatusBarPanel> statusBarActions = new List<StatusBarPanel>();
        private ToolStripItem tsdi1;
        private ContainerControl refbox, refboxparent, framelistparent, scenelistparent;
        private FlowLayoutPanel framelist, scenelist;
        private Form cbx; /*campvw (Camera Preview)*/
        private Font stdFont = new Font("Segoe UI Emoji", 10);
        /*OLD
        //private DocumentMeta openDoc;
        //private Document doc;
        */

        private DialogResult Debug(string text) { return Debug(text, "Debug"); }
        private DialogResult Debug(string text, string title) { return MessageBox.Show(text, title); }

        public Needle()
        {
            InitializeComponent();
            //this.ResizeEnd += new EventHandler(RedrawUi);
            this.InitializeUi(true);
            this.DrawUi();
        }
        public Needle(string pathToOpen)
        {
            InitializeComponent();
            //this.ResizeEnd += new EventHandler(RedrawUi);
            this.InitializeUi(false);
            this.DrawUi();
            this.Open(pathToOpen);
        }

        private void InitializeUi(bool showLauncher)
        {
            /*// WINDOW/APP //*/
            this.Font = stdFont;
            this.Text = "Stitcher " + stitcherVersion;
            this.Icon = new Icon("img/stitcher.ico");
            this.KeyPreview = true;
            this.KeyDown += (object sender, KeyEventArgs e) => { if (selectImage && e.KeyCode == Keys.Escape) { ExitAddFrame(); } };
            /*// MENU BAR //*/
            menuStrip1 = new MenuStrip() { Dock = DockStyle.Top };
            ToolStripMenuItem filemenu = new ToolStripMenuItem("File"), videomenu = new ToolStripMenuItem("Video"), viewmenu = new ToolStripMenuItem("View");
            filemenu.DropDownItems.Add("New", (Image)null, new EventHandler(NewVid));
            filemenu.DropDownItems.Add("Open", (Image)null, new EventHandler(OpenFile));
            filemenu.DropDownItems.Add(new ToolStripSeparator());
            tsdi1 = filemenu.DropDownItems.Add("Save", (Image)null, new EventHandler(SaveCnm));
            filemenu.DropDownItems.Add("Save As", (Image)null, new EventHandler(SaveAs));
            filemenu.DropDownItems.Add(new ToolStripSeparator());
            filemenu.DropDownItems.Add("Project Settings", (Image)null, new EventHandler(ProjectSettings));
            filemenu.DropDownItems.Add("Export", (Image)null, new EventHandler(ExportVideo));
            filemenu.DropDownItems.Add(new ToolStripSeparator());
            filemenu.DropDownItems.Add("Configuration", (Image)null, new EventHandler(ShowConfig));
            filemenu.DropDownItems.Add(new ToolStripSeparator());
            filemenu.DropDownItems.Add("Quit", (Image)null, new EventHandler(QuitApp));
            ToolStripMenuItem framemenu = new ToolStripMenuItem("Import Image...");
            framemenu.DropDownItems.Add("From Camera", (Image)null, new EventHandler(CameraFrame));
            framemenu.DropDownItems.Add("From File", (Image)null, new EventHandler(FileFrame));
            videomenu.DropDownItems.Add(framemenu);
            videomenu.DropDownItems.Add("New Scene", (Image)null, new EventHandler(NewScene));
            videomenu.DropDownItems.Add("New Frame", (Image)null, new EventHandler(AddFrame));
            //videomenu.DropDownItems.Add("New Frame", (Image)null, new EventHandler(NewFrame));
            videomenu.DropDownItems.Add(new ToolStripSeparator());
            videomenu.DropDownItems.Add("Export", (Image)null, new EventHandler(ExportVideo));
            viewmenu.DropDownItems.Add("Reload Data", (Image)null, new EventHandler(UpdateEdit));
            viewmenu.DropDownItems.Add("Refresh view", (Image)null, new EventHandler(RedrawUi));
            ToolStripMenuItem helpbutt = new ToolStripMenuItem("Help", (Image) null, new EventHandler(ShowHelp));
            menuStrip1.Items.Add(filemenu);
            menuStrip1.Items.Add(viewmenu);
            menuStrip1.Items.Add(videomenu);
            menuStrip1.Items.Add(helpbutt);
            this.MainMenuStrip = menuStrip1;
            this.Controls.Add(menuStrip1);
            /*// STATUSBAR //*/
            statusBar = new StatusBar() {
                Dock = DockStyle.Bottom,
                SizingGrip = false,
                ShowPanels = true
            };
            statusBarDate = new StatusBarPanel()
            {
                BorderStyle = StatusBarPanelBorderStyle.Raised,
                Text = DateTime.Now.ToShortDateString(),
                ToolTipText = "Application start date",
                AutoSize = StatusBarPanelAutoSize.Contents,
                Alignment = HorizontalAlignment.Right
            };
            statusBarInstruction = new StatusBarPanel()
            {
                BorderStyle = StatusBarPanelBorderStyle.Raised,
                Text = "Ready",
                ToolTipText = "Application status",
                AutoSize = StatusBarPanelAutoSize.Spring
            };
            statusBarActions.Add(new StatusBarPanel()
            {
                ToolTipText = "Export video",
                BorderStyle = StatusBarPanelBorderStyle.Raised,
                Alignment = HorizontalAlignment.Center,
                Icon = new Icon("img/export.ico", 32, 32),
                AutoSize = StatusBarPanelAutoSize.Contents
            });
            statusBarActions.Add(new StatusBarPanel()
            {
                ToolTipText = "Save project",
                BorderStyle = StatusBarPanelBorderStyle.Raised,
                Alignment = HorizontalAlignment.Center,
                Icon = new Icon("img/save.ico", 32, 32),
                AutoSize = StatusBarPanelAutoSize.Contents
            });
            foreach (StatusBarPanel statusBarActionPanel in statusBarActions) statusBar.Panels.Add(statusBarActionPanel);
            statusBar.Panels.Add(statusBarInstruction);
            statusBar.Panels.Add(statusBarDate);
            this.Controls.Add(statusBar);
            statusBar.PanelClick += (object sender, StatusBarPanelClickEventArgs e) => {
                string cpttt = e.StatusBarPanel.ToolTipText;
                if (cpttt == "Export video") ExportVideo();
                else if (cpttt == "Save project") SaveCnm();
                else if (cpttt == "Open project") OpenFile();
                else if (cpttt == "New project") NewVid();
            };
            /*// SETTINGS //*/

            /*// REFERENCES //*/
            refboxparent = new ContainerControl()
            {
                BackColor = Color.White,
                Left = 10,
                Top = 10 + 25,
                AutoScroll = false
            };
            refbox = new ContainerControl()
            {
                BackColor = Color.White,
                Left = 2,
                Top = 22,
                AutoScroll = true
            };
            this.Controls.Add(refboxparent);
            refboxparent.Controls.Add(refbox);
            refbox.HorizontalScroll.Enabled = false;
            refbox.HorizontalScroll.Visible = false;
            refboxparent.Controls.Add(new Label()
            {
                Text = "Open Images",
                Left = 2,
                Top = 2,
                Width = refboxparent.Width - 26,
                Height = 20
            });
            refboxparent.Controls.Add(new Button()
            {
                Text = "+",
                Left = refboxparent.Width - 22,
                Top = 2,
                Width = 20,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            });
            ((Button)refboxparent.Controls[refboxparent.Controls.Count - 1]).Click += new EventHandler(NewFrame);
            /*// VIEWPORT //*/
            //Inconspicuous empty space
            
            /*// TIMELINE //*/

            framelistparent = new ContainerControl()
            {
                BackColor = Color.White,
                Left = 10,
                Top = (this.Height / 5 * 3) - 10,
                AutoScroll = false,
                Size = new Size(this.Width - 40, (this.Height / 10 * 3) - 30)
            };
            framelist = new FlowLayoutPanel()
            {
                BackColor = Color.White,
                Left = 2,
                Top = 22,
                AutoScroll = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AllowDrop = true
            };
            this.Controls.Add(framelistparent);
            framelistparent.Controls.Add(framelist);
            framelistnewbutt = new Button()
            {
                Text = "+",
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 20,
                Height = 20,
                Top = 2
            };
            framelistnewbutt.Click += AddFrame;
            framelistparent.Controls.Add(framelistnewbutt);
            framelistparent.Controls.Add(new Label() { Text = "Scene frames", Size = new Size(framelistparent.Width - 4, 20), Location = new Point(2, 2) });
            HorizontalScrollBars(framelist);
            framelist.DragOver += (object sender, DragEventArgs e) => {
                if (e.Data.GetDataPresent(typeof(string))) e.Effect = DragDropEffects.Copy;
            };
            framelist.DragDrop += (object sender, DragEventArgs e) => {
                if (e.Data.GetDataPresent(typeof(string)))
                {
                    //MessageBox.Show("Debug: " + e.Data.GetData(typeof(string)).ToString());
                    AddFrame(e.Data.GetData(typeof(string)).ToString());
                } else
                {
                    MessageBox.Show("The format of the dropped file is not valid. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            /*// SCENES //*/
            scenelistparent = new ContainerControl()
            {
                BackColor = Color.White,
                Left = 10,
                AutoScroll = false
            };
            scenelist = new FlowLayoutPanel()
            {
                BackColor = Color.White,
                Left = 2,
                Top = 22,
                AutoScroll = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            this.Controls.Add(scenelistparent);
            scenelistparent.Controls.Add(scenelist);
            scenelistnewbutt = new Button()
            {
                Text = "+",
                Size = new Size(20, 20),
                Top = 2,
                TextAlign = ContentAlignment.MiddleCenter
            };
            scenelistnewbutt.Click += NewScene;
            scenelistparent.Controls.Add(scenelistnewbutt);
            scenelistparent.Controls.Add(new Label() { Text = "Scenes", Location = new Point(2, 2) });
            HorizontalScrollBars(scenelist);
            /*// LAUNCHER //*/
            if (showLauncher)
            {
                launcher = new Form() { Width = 182, Height = 123, Text = "Start", ControlBox = false, ShowIcon = false, Icon = new Icon("img/stitcher.ico"), SizeGripStyle = SizeGripStyle.Hide };
                launcher.Controls.Add(new Button() { Text = "New project", DialogResult = DialogResult.OK, Height = 30, Width = 160, Top = 19, Left = 2 });
                ((Button)launcher.Controls[launcher.Controls.Count - 1]).Click += new EventHandler(NewVid);
                ((Button)launcher.Controls[launcher.Controls.Count - 1]).Click += new EventHandler(DestroyParent);
                launcher.Controls.Add(new Button() { Text = "Open project", DialogResult = DialogResult.OK, Height = 30, Width = 160, Top = 51, Left = 2 });
                ((Button)launcher.Controls[launcher.Controls.Count - 1]).Click += new EventHandler(OpenFile);
                ((Button)launcher.Controls[launcher.Controls.Count - 1]).Click += new EventHandler(DestroyParent);
                launcher.Controls.Add(new Label() { Text = "Welcome to Stitcher!", Height = 15, Width = 160, Left = 2, Top = 2 });
                launcher.ShowDialog(this);
                launcher.Dispose();
            }
            /*// Fix TwoRedraw and RBI bugs //*/
            this.SizeChanged += new EventHandler(RedrawUi);
        }

        private void DestroyParent(object sender, EventArgs e)
        {
            ((Button)sender).FindForm().Close();
            ((Button)sender).FindForm().Dispose();
        }
        private void RedrawUi(object sender, EventArgs e) { DrawUi(); }
        private void DrawUi()
        {
            /*// REFERENCES //*/
            refboxparent.Size = new Size((this.Width / 3) - 40, (this.Height / 5 * 2) - 55);
            refbox.Size = new Size(refboxparent.Width - 4, refboxparent.Height - 26);
            refboxparent.Controls[refboxparent.Controls.Count - 1].Left = refboxparent.Width - 22;
            refboxparent.Controls[refboxparent.Controls.Count - 2].Width= refboxparent.Width - 26;
            ResizeChildWidth(refbox);
            VerticalScrollBars(refbox);
            /*// TIMELINE //*/
            framelistparent.Size = new Size(this.Width - 40, (this.Height / 10 * 3) - 30);
            framelistparent.Top = (this.Height / 5 * 2) - 10;
            framelist.Size = new Size(this.Width - 44, (this.Height / 10 * 3) - 54);
            framelistnewbutt.Left = framelistparent.Width - 22;
            HorizontalScrollBars(framelist);
            foreach (Control frameTile in framelist.Controls)
            {
                ((FrameTile)frameTile).stdHeight = (this.Height / 10 * 3) - 56 - 20;
                ((FrameTile)frameTile).Draw();
            }
            /*// SCENELIST //*/
            scenelistparent.Size = new Size(this.Width - 40, (this.Height / 10 * 3) - 30);
            scenelistparent.Top = (this.Height / 10 * 7) - 25;
            scenelist.Size = new Size(this.Width - 44, (this.Height / 10 * 3) - 54);
            scenelistnewbutt.Left = scenelistparent.Width - 22;
            HorizontalScrollBars(scenelist);
            foreach (Control sceneTile in scenelist.Controls)
            {
                ((Tile)sceneTile).stdHeight = (this.Height / 10 * 3) - 56 - 20;
                ((Tile)sceneTile).Draw();
            }
        }
        private void ShowHelp(object sender, EventArgs e)
        {
            // ""Help""
            MessageBox.Show("no", "Stitcher Help");
        }
        private void HorizontalScrollBars(ScrollableControl ctrl) { ScrollBars(ctrl, ctrl.VerticalScroll); } //LEGACY SUPPORT
        private void VerticalScrollBars(ScrollableControl ctrl) { ScrollBars(ctrl, ctrl.HorizontalScroll); } //LEGACY SUPPORT
        private void ScrollBars(ScrollableControl ct, ScrollProperties ctrl)
        {
            ct.AutoScroll = false;  //I don't know why this works,
            ctrl.Enabled = false;   //I don't understand how this works,
            ctrl.Visible = false;   //But somehow, it does.
            ctrl.Maximum = 0;       //So here it shall stay.
            ct.AutoScroll = true;   //I HATE UI DESIGN!!!!
        }
        private void ResizeChildWidth(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                child.Width = parent.Width - (child.Left * 2);
                child.MaximumSize = new Size(child.Width, 42);
            }
        }
        private void UpdateEdit(object sender, EventArgs e) { UpdateEdit(); }
        private void UpdateEdit()
        {
            framelist.Controls.Clear();
            scenelist.Controls.Clear();
            int sceneNum = 0, scfi;
            foreach (List<string> scene in film)
            {
                //SCENE
                scenelist.Controls.Add(new Tile((scene.Count == 0 ? null : scene[0]), (Tile tile, bool dir) =>
                {
                    int sceneIndex = tile.index;
                    film.RemoveAt(sceneIndex);
                    try
                    {
                        film.Insert(sceneIndex + (dir ? 1 : -1), scene);
                    } catch(ArgumentOutOfRangeException err)
                    {
                        MessageBox.Show("Index out of range: cannot move scene outside of film bounds.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        film.Insert(sceneIndex, scene);
                    }
                    UpdateEdit();
                    DrawUi();
                }, (Tile tile) =>
                {
                    tile.DestroyTile();
                    film.RemoveAt(tile.index);
                    UpdateEdit();
                    DrawUi();
                }, (Tile tile) =>
                {
                    openScene = tile.index;
                    UpdateEdit();
                    DrawUi();
                }) { index = sceneNum });

                scfi = 0;
                foreach(string frame in scene)
                {
                    //FRAME
                    if (sceneNum == openScene)
                    {
                        //CURRENT
                        framelist.Controls.Add(new FrameTile(frame, (FrameTile tile, bool dir) =>
                        {
                            int frameIndex = tile.index;
                            scene.RemoveAt(frameIndex);
                            try
                            {
                                scene.Insert(frameIndex + (dir ? 1 : -1), frame);
                            }
                            catch (ArgumentOutOfRangeException err)
                            {
                                MessageBox.Show("Index out of range: cannot move frame outside of scene bounds.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                scene.Insert(frameIndex, frame);
                            }
                            UpdateEdit();
                            DrawUi();
                        }, (FrameTile tile) =>
                        {
                            tile.DestroyTile();
                            scene.RemoveAt(tile.index);
                            DrawUi();
                        }, (FrameTile tile) =>
                        {
                            //EDIT FRAME
                            MessageBox.Show("Frame editing will be added soon.");
                        })
                        { index = scfi });
                    }
                    scfi++;
                }
                sceneNum++;
            }
        }
        private void SetHighlight(Control ctrl, bool highlight)
        {
            //ctrl.BackColor = highlight ? Color.FromArgb(238, 238, 238) : Color.White;
            Color flashColour =
                //Color.FromArgb(238, 238, 238);
                //Color.Yellow;
                Color.LightYellow;

            new System.Threading.Thread(() => {
                if (highlight)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        ctrl.BackColor = flashColour;
                        System.Threading.Thread.Sleep(80);
                        ctrl.BackColor = Color.White;
                        System.Threading.Thread.Sleep(80);
                    }
                }
                //else ctrl.BackColor = Color.White;
            }).Start();
        }
        private void ExitAddFrame()
        {
            statusBarInstruction.Text = "Ready";
            selectImage = false;
            SetHighlight(refbox, false);
        }
        private void StartAddFrame()
        {
            statusBarInstruction.Text = "Select an image to add to the scene... (Esc to cancel)";
            selectImage = true;
            SetHighlight(refbox, true);
        }
        private void AddFrame(object sender, EventArgs e) { StartAddFrame(); }
        private void AddFrame(string path) { if (film.Count == 0) return; AddFrame(path, openScene); }
        private void AddFrame(string path, int scene) { if (film.Count <= scene) return; AddFrame(path, scene, film[scene].Count); }
        private void AddFrame(string path, int scene, int frame)
        {
            film[scene].Insert(frame, path);
            UpdateEdit();
            DrawUi();
        }
        private void NewVid(object sender, EventArgs e) { NewVid(); }
        private void NewVid()
        {
            if (MessageBox.Show("Any unsaved changes will be lost. Continue?", "New Project", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) != DialogResult.Yes) return;
            Button sub = new Button() { Text = "Create", Location = new Point(50, 55), Size = new Size(70, 25) }, canx = new Button() { Text = "Cancel", Location = new Point(125, 55), Size = new Size(70, 25) };
            TextBox pnm = new TextBox() { PlaceholderText="Project name", Location = new Point(5, 5), Size = new Size(190, 20) }, fnm = new TextBox() { PlaceholderText="Film Name", Location = new Point(5, 30), Size = new Size(190, 20) };
            cbx = new Form() { Text = "New Project", ControlBox = false, ShowIcon = false, ShowInTaskbar = false, Size = new Size(215, 125), SizeGripStyle = SizeGripStyle.Hide };
            cbx.Controls.Add(pnm);
            cbx.Controls.Add(fnm);
            cbx.Controls.Add(canx);
            cbx.Controls.Add(sub);
            sub.Click += new EventHandler(NewFile);
            canx.Click += new EventHandler(CloseCBX);
            cbx.AcceptButton = sub;
            cbx.CancelButton = canx;
            cbx.ShowDialog();
        }
        private void SaveCnm(object sender, EventArgs e) { SaveCnm(); }
        private void SaveCnm()
        {
            if (meta == null)
            {
                MessageBox.Show("No open document to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            statusBarInstruction.Text = "Saving...";
            if (meta["savelocation"] != null) {
                Save(meta["savelocation"]);
            } else {
                //MessageBox.Show("The Document could not be found. Please save the document with a name first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SaveAs();
            }
            statusBarInstruction.Text = "Ready";
        }
        private void SaveAs(object sender, EventArgs e) { SaveAs(); }
        private void SaveAs()
        {
            if (meta == null)
            {
                MessageBox.Show("No open document to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            statusBarInstruction.Text = "Saving...";
            SaveFileDialog sfdialog = new SaveFileDialog();
            sfdialog.Filter = "Stitcher files (*.ndls)|*.ndls;*.stitchdoc;*.stcmf";
            if (sfdialog.ShowDialog() == DialogResult.OK)
            {
                if (sfdialog.FileName != null) Save(sfdialog.FileName);
            }
            statusBarInstruction.Text = "Ready";
        }
        private void Save(string path)
        {
            meta.Remove("savelocation");
            meta.Add("savelocation", path);
            System.IO.FileStream sffs = System.IO.File.Open(path, System.IO.FileMode.OpenOrCreate);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binf =
                new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            binf.Serialize(sffs, new SavedDocument(this));
            sffs.Close();
            tsdi1.Enabled = true;
        }
        private void OpenFile(object sender, EventArgs e) { OpenFile(); }
        private void OpenFile()
        {
            statusBarInstruction.Text = "Opening file...";
            OpenFileDialog ofdialog = new OpenFileDialog();
            ofdialog.Filter = "Stitcher files (*.ndls)|*.ndls;*.stitchdoc;*.stcmf";
            if (ofdialog.ShowDialog() == DialogResult.OK)
            {
                if (System.IO.File.Exists(ofdialog.FileName)) Open(ofdialog.FileName);
            }
            statusBarInstruction.Text = "Ready";
        }
        private void Open(string path)
        {
            try
            {
                System.IO.FileStream offsxt = System.IO.File.Open(path, System.IO.FileMode.Open);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binfxt =
                    new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                Tuple<Dictionary<string, string>, List<List<string>>> odocxt = ((SavedDocument)binfxt.Deserialize(offsxt)).ToDoc();
                offsxt.Close();
            } catch (InvalidCastException err)
            {
                MessageBox.Show("The document you attempted to open is either formatted in an earlier version of the Stitcher Binary Format (you are using " + fileFormatVersion + "), invalid, from another program, or corrupted. This is unrecoverable.", "Unrecoverable error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                throw new Exception("File format error: the requested file was not formatter correctly.");
            }
            System.IO.FileStream offs = System.IO.File.Open(path, System.IO.FileMode.Open);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binf =
                new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            Tuple<Dictionary<string, string>, List<List<string>>> odoc = ((SavedDocument)binf.Deserialize(offs)).ToDoc();
            this.meta = odoc.Item1;
            this.film = odoc.Item2;
            offs.Close();
            this.Text = meta["projectname"] + " - Stitcher " + stitcherVersion;
            openScene = 0;
            UpdateEdit();
        }
        private void NewFile(object sender, EventArgs e) {
            statusBarInstruction.Text = "Creating file...";
            Button cbt = ((Button)sender);
            Form pfm = cbt.FindForm();
            meta = new Dictionary<string, string>();
            meta.Add("projectname", pfm.Controls[0].Text);
            this.Text = pfm.Controls[0].Text + " - Stitcher " + stitcherVersion;
            meta.Add("filmname", pfm.Controls[1].Text);
            meta.Add("savelocation", null);
            film = new List<List<string>>();
            film.Add(new List<string>());
            openScene = 0;
            UpdateEdit();
            DrawUi();
            pfm.Close();
            statusBarInstruction.Text = "Ready";
        }
        private void AddToSources(string path)
        {
            refs.Add(path);
            string fn = path.Split(new char[] { '\\' })[path.Split(new char[] { '\\' }).Length - 1];
            Label nit = new Label() {
                Text = fn + ": " + path/*(path.Length > 30 ? path.Substring(0, 30) + "..." : path)*/,
                Location = new Point(3, refcts.Count * 44),
                Size = new Size(refbox.Width - 10, 20),
                MaximumSize = new Size(refbox.Width - 10, 42),
                AutoSize = true,
                AutoEllipsis = true
            };
            int reflox = refcts.Count;
            refcts.Add(new Tuple<Label, Rectangle>(nit, new Rectangle()));
            refbox.Controls.Add(nit);
            nit.MouseDown += (object sender, MouseEventArgs e) =>
            {
                if (selectImage)
                {
                    AddFrame(path);
                    ExitAddFrame();
                    return;
                }
                Size dragSize = SystemInformation.DragSize;
                refcts[reflox] = new Tuple<Label, Rectangle>(nit, new Rectangle(new Point(e.X - (dragSize.Width / 2), e.Y - (dragSize.Height / 2)), dragSize));
            };
            nit.MouseUp += (object sender, MouseEventArgs e) =>
            {
                refcts[reflox] = new Tuple<Label, Rectangle>(nit, Rectangle.Empty);
            };
            nit.MouseMove += (object sender, MouseEventArgs e) =>
            {
                if ((e.Button & MouseButtons.Left) == MouseButtons.Left) if (refcts[reflox].Item2 != Rectangle.Empty && !refcts[reflox].Item2.Contains(e.X, e.Y)) nit.DoDragDrop(path, DragDropEffects.Copy);
            };
            DrawUi();
        }
        private void NewScene(object sender, EventArgs e) { NewScene(); }
        private void NewScene()
        {
            if (meta == null)
            {
                MessageBox.Show("No open document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            film.Add(new List<string>());
            UpdateEdit();
            DrawUi();
        }
        private void NewFrame(object sender, EventArgs e)
        {
            if (meta == null)
            {
                MessageBox.Show("No open document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult chosenopt = MessageBox.Show("Would you like to create a new image from the camera?", "New Frame", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (chosenopt == DialogResult.Yes)
            {
                CameraFrame();
            } else if (chosenopt == DialogResult.No)
            {
                FileFrame();
            }
        }
        private void CameraFrame(object sender, EventArgs e) { CameraFrame(); }
        async private Task CameraFrame()
        {
            if (meta == null)
            {
                MessageBox.Show("No open document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (videoDeviceMoniker == "") { MessageBox.Show("A camera has not been detected. Please set this up in the Configuration menu.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            statusBarInstruction.Text = "Taking photo...";
            if (MessageBox.Show("Press OK to take photo.", "Camera", MessageBoxButtons.OKCancel, MessageBoxIcon.None) == DialogResult.Cancel) return;
            //FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            vsource = new VideoCaptureDevice(videoDeviceMoniker);
            vsource.NewFrame += new AForge.Video.NewFrameEventHandler(CamNewFrame);
            string photoSaveLocationCache = photoSaveLocation;
            vsource.Start();
            while (photoSaveLocationCache == photoSaveLocation)
            {
                Console.WriteLine("Waiting");
                await Task.Delay(100);
            }
            AddToSources(photoSaveLocation);
            statusBarInstruction.Text = "Ready";
        }
        private void CamNewFrame(object sender, AForge.Video.NewFrameEventArgs e)
        {
            vsource.SignalToStop();
            Bitmap takenphoto = e.Frame;
            string cdcidfpn = System.IO.Directory.GetCurrentDirectory() + "\\TakenImages\\" + meta["projectname"];
            if (!System.IO.Directory.Exists(cdcidfpn)) System.IO.Directory.CreateDirectory(cdcidfpn);
            string bmpfn = cdcidfpn + "\\" + (System.IO.Directory.GetFiles(cdcidfpn).Length + 1).ToString() + ".jpeg";
            if (System.IO.File.Exists(bmpfn)) return;
            takenphoto.Save(bmpfn, System.Drawing.Imaging.ImageFormat.Jpeg);
            photoSaveLocation = bmpfn; //AddToSources(bmpfn);
        }
        private void FileFrame(object sender, EventArgs e) { FileFrame(); }
        private void FileFrame()
        {
            if (meta == null)
            {
                MessageBox.Show("No open document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            OpenFileDialog fsdialog = new OpenFileDialog() {
                Filter = "Image Files|*.jpg;*.png;*.bmp|All files (*.*)|*.*"
            };
            if (fsdialog.ShowDialog() == DialogResult.OK)
            {
                if (System.IO.File.Exists(fsdialog.FileName))
                {
                    if (fsdialog.FileName.Length > 999)
                    {
                        MessageBox.Show("File path is too long: paths are limited to a length of 999 characters. Please try moving your file closer to the root directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    AddToSources(fsdialog.FileName);
                }
            }
        }
        private void ExportVideo(object sender, EventArgs e) { ExportVideo(); }
        private void ExportVideo()
        {
            if (meta == null)
            {
                MessageBox.Show("No open document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            statusBarInstruction.Text = "Exporting video...";
            if (MessageBox.Show("Would you like to save before export?", "Export", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) SaveCnm();

            Button sub = new Button() { Text = "Submit", Location = new Point(50, 55), Size = new Size(70, 25) }, canx = new Button() { Text = "Cancel", Location = new Point(125, 55), Size = new Size(70, 25) };
            TextBox pnm = new TextBox() { PlaceholderText = "Resolution", Location = new Point(5, 5), Size = new Size(190, 20) }, fnm = new TextBox() { PlaceholderText = "Framerate", Location = new Point(5, 30), Size = new Size(190, 20) };
            cbx = new Form() { Text = "Export", ControlBox = false, ShowIcon = false, ShowInTaskbar = false, Size = new Size(215, 125), SizeGripStyle = SizeGripStyle.Hide };
            cbx.Controls.Add(pnm);
            cbx.Controls.Add(fnm);
            cbx.Controls.Add(canx);
            cbx.Controls.Add(sub);
            sub.Click += new EventHandler(Export);
            canx.Click += new EventHandler(CloseCBX);
            cbx.AcceptButton = sub;
            cbx.CancelButton = canx;
            cbx.ShowDialog();
        }
        private void Export(object sender, EventArgs e)
        {
            Form expfm = ((Button)sender).FindForm();
            string[] res = expfm.Controls[0].Text.Split(new char[] { 'x' });
            CloseCBX(sender, e);
            //Actual stuff
            SaveFileDialog exdialog = new SaveFileDialog();
            exdialog.Filter = "Supported Video Files|*.mp4;*.mp2;*.flv;*.raw|All files (*.*)|*.*";
            if (exdialog.ShowDialog() != DialogResult.OK) return;
            cbx = new Form();
            cbx.Controls.Add(new ProgressBar());
            foreach (List<string> scene in film) ((ProgressBar)cbx.Controls[0]).Maximum += scene.Count;
            ((ProgressBar)cbx.Controls[0]).Maximum *= 2;
            string selloc = exdialog.FileName;
            VideoCodec usedcodec = VideoCodec.MPEG4;
            if (selloc.EndsWith(".mp4")) usedcodec = VideoCodec.MPEG4; //USE SWITCH-CASE HERE!
            else if (selloc.EndsWith(".mp2")) usedcodec = VideoCodec.MPEG2;
            else if (selloc.EndsWith(".flv")) usedcodec = VideoCodec.FLV1;
            else if (selloc.EndsWith(".raw")) usedcodec = VideoCodec.Raw;
            else selloc += ".mp4";
            VideoFileWriter exporter = new VideoFileWriter();
            try
            {
                exporter.Open(selloc, Int32.Parse(res[0]), Int32.Parse(res[1]), Int32.Parse(expfm.Controls[1].Text), usedcodec);
            } catch(Exception err)
            {
                MessageBox.Show("An error occurred whilst exporting your video. Message:\n" + err.ToString(), "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ((ProgressBar)cbx.Controls[0]).Value = ((ProgressBar)cbx.Controls[0]).Maximum / 2;
            int framenumber = 0;
            foreach (List<string> scene in film)
            {
                foreach (string frameloc in scene)
                {
                    try
                    {
                        exporter.WriteVideoFrame(new Bitmap(frameloc));
                        ((ProgressBar)cbx.Controls[0]).Value += 1;
                        statusBarInstruction.Text = "Exporting: frame " + framenumber.ToString();
                        framenumber += 1;
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show("An error occurred whilst exporting frame '" + frameloc + "'. Message:\n" + err, "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            cbx.Close();
            exporter.Close();
            MessageBox.Show("Video exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            statusBarInstruction.Text = "Ready";
        }
        private void ShowConfig(object sender, EventArgs e) { ShowConfig(); }
        private DialogResult ShowConfig()
        {
            Form cfgdl = new Form()
            {
                ShowIcon = false,
                ShowInTaskbar = false,
                Text = "Configuration",
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(340, 120),
                SizeGripStyle = SizeGripStyle.Hide
            };
            ComboBox spinbox = new ComboBox()
            {
                Size = new Size(300, 25),
                Location = new Point(10, 10)
            };
            foreach (FilterInfo device in new FilterInfoCollection(FilterCategory.VideoInputDevice))
            {
                int idex = spinbox.Items.Add(device.MonikerString);
                if (videoDeviceMoniker == device.MonikerString) spinbox.SelectedIndex = idex;
            }
            Button save = new Button()
            {
                Text = "Save",
                Size = new Size(100, 25),
                Location = new Point(210, 45)
            };
            save.Click += (object sender, EventArgs e) =>
            {
                videoDeviceMoniker = spinbox.SelectedItem.ToString();
                cfgdl.DialogResult = DialogResult.OK;
                cfgdl.Close();
            };
            cfgdl.Controls.Add(new Button()
            {
                Text = "Cancel",
                Size = new Size(100, 25),
                Location = new Point(100, 45),
                DialogResult = DialogResult.Cancel
            });
            cfgdl.Controls.Add(save);
            cfgdl.Controls.Add(spinbox);
            return cfgdl.ShowDialog();
        }
        private void ProjectSettings(object sender, EventArgs e) { ProjectSettings(); }
        private void ProjectSettings()
        {
            if (meta == null)
            {
                MessageBox.Show("No open document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Button sub = new Button() { Text = "Save", Location = new Point(50, 55), Size = new Size(70, 25) }, canx = new Button() { Text = "Cancel", Location = new Point(125, 55), Size = new Size(70, 25) };
            TextBox pnm = new TextBox() { PlaceholderText = "Project name", Location = new Point(5, 5), Size = new Size(190, 20) }, fnm = new TextBox() { PlaceholderText = "Film Name", Location = new Point(5, 30), Size = new Size(190, 20) };
            cbx = new Form() { Text = "Edit Project", ControlBox = false, ShowIcon = false, ShowInTaskbar = false, Size = new Size(215, 125), SizeGripStyle = SizeGripStyle.Hide };
            cbx.Controls.Add(pnm);
            cbx.Controls.Add(fnm);
            cbx.Controls.Add(canx);
            cbx.Controls.Add(sub);
            sub.Click += new EventHandler(SaveMeta);
            canx.Click += new EventHandler(CloseCBX);
            cbx.AcceptButton = sub;
            cbx.CancelButton = canx;
            cbx.ShowDialog();
        }
        private void SaveMeta(object sender, EventArgs e)
        {
            Form originfm = ((Button)sender).FindForm();
            meta.Remove("projectname");
            meta.Remove("filmname");
            meta.Add("projectname", originfm.Controls[0].Text);
            meta.Add("filmname", originfm.Controls[1].Text);
            CloseCBX(sender, e);
            if (meta["savelocation"] != null) SaveCnm(sender, e); /*Autosave*/
            this.Text = meta["projectname"] + " - Stitcher " + stitcherVersion;
        }
        private void CloseCBX(object sender, EventArgs e) { cbx.Close(); }
        private void QuitApp(object sender, EventArgs e) { QuitApp(); }
        private void QuitApp() {
            if (MessageBox.Show("Unsaved changes will be lost. Do you wish to continue?", "Quit App", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel) return;
            if (vsource != null) if (vsource.IsRunning) vsource.SignalToStop();
            this.Dispose();
            Application.Exit();
        }
    }

    public class Tile : Panel
    {
        public int stdWidth = 160, stdHeight = 120, index;
        private string _image;
        public string image
        {
            get { return _image; }
            set
            {
                _image = value;
                imagePreview.Image = Image.FromFile(value);
            }
        }
        private Label imagePreview;
        private Button deleteButt, selectButt;
        private Tuple<Button, Button> moveButts;
        private GroupBox ctct;

        public Tile(string imagePath, Action<Tile, bool> moveHandler, Action<Tile> deleteHandler, Action<Tile> selectionHandler)
        {
            Size = new Size(stdWidth + 4, stdHeight + 4);
            ctct = new GroupBox()
            {
                Size = new Size(stdWidth, stdHeight),
                Location = new Point(2, 2)
            };
            Controls.Add(ctct);
            imagePreview = new Label()
            {
                Width = stdWidth - 4,
                Height = stdHeight - 26,
                Image = Image.FromFile(imagePath == null ? "blank.jpg" : imagePath),
                Location = new Point(2, 2),
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleCenter
            };
            deleteButt = new Button()
            {
                Text = "🗑️",
                Size = new Size(20, 20),
                Location = new Point(stdWidth - 22, stdHeight - 22)
            };
            selectButt = new Button()
            {
                Text = "Open",
                Size = new Size(50, 20),
                Location = new Point((stdWidth / 2) - 25, stdHeight - 22)
            };
            moveButts = new Tuple<Button, Button>(new Button()
            {
                Text = "<",
                Size = new Size(20, 20),
                Location = new Point(2, stdHeight - 22)
            }, new Button()
            {
                Text = ">",
                Size = new Size(20, 20),
                Location = new Point(24, stdHeight - 22)
            });
            deleteButt.Click += (object sender, EventArgs e) => { deleteHandler(this); };
            selectButt.Click += (object sender, EventArgs e) => { selectionHandler(this); };
            moveButts.Item1.Click += (object sender, EventArgs e) => { moveHandler(this, false); };
            moveButts.Item2.Click += (object sender, EventArgs e) => { moveHandler(this, true); };
            ctct.Controls.Add(imagePreview);
            ctct.Controls.Add(deleteButt);
            ctct.Controls.Add(selectButt);
            ctct.Controls.Add(moveButts.Item1);
            ctct.Controls.Add(moveButts.Item2);
            image = (imagePath == null ? "blank.jpg" : imagePath);
            this.Draw();
        }

        public void Draw()
        {
            imagePreview.Size = new Size(stdWidth - 4, stdHeight - 26);
            deleteButt.Location = new Point(stdWidth - 22, stdHeight - 22);
            selectButt.Location = new Point((stdWidth / 2) - 25, stdHeight - 22);
            moveButts.Item1.Top = stdHeight - 22;
            moveButts.Item2.Top = stdHeight - 22;
            ctct.Size = new Size(stdWidth, stdHeight);
            Size = new Size(stdWidth + 4, stdHeight + 4);
        }

        public void DestroyTile()
        {
            Parent.Controls.Remove(this);
        }
    }

    public class FrameTile : Panel
    {
        public int stdWidth = 160, stdHeight = 120, index;
        private string _image;
        public string image {
            get { return _image; }
            set {
                _image = value;
                imagePreview.Text = value.Substring(value.LastIndexOf('\\') + 1);
                imagePreview.Image = Image.FromFile(value);
            }
        }
        private Label imagePreview;
        private Button deleteButt, selectButt, infoButt;
        private Tuple<Button, Button> moveButts;
        private GroupBox ctct;

        public FrameTile(string imagePath, Action<FrameTile, bool> moveHandler, Action<FrameTile> deleteHandler, Action<FrameTile> selectionHandler) {
            Size = new Size(stdWidth + 4, stdHeight + 4);
            ctct = new GroupBox()
            {
                Size = new Size(stdWidth, stdHeight),
                Location = new Point(2, 2)
            };
            Controls.Add(ctct);
            imagePreview = new Label()
            {
                Text = imagePath.Substring(imagePath.LastIndexOf('\\') + 1),
                Width = stdWidth - 4,
                Height = stdHeight - 26,
                Image = Image.FromFile(imagePath),
                Location = new Point(2, 2),
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleCenter
            };
            deleteButt = new Button()
            {
                Text = "🗑️",
                Size = new Size(20, 20),
                Location = new Point(stdWidth - 22, stdHeight - 22)
            };
            infoButt = new Button()
            {
                Text = "?",
                Size = new Size(20, 20),
                Location = new Point(stdWidth - 44, stdHeight - 22)
            };
            selectButt = new Button()
            {
                Text = "Edit",
                Size = new Size(50, 20),
                Location = new Point((stdWidth / 2) - 25, stdHeight - 22)
            };
            moveButts = new Tuple<Button, Button>(new Button()
            {
                Text = "<",
                Size = new Size(20, 20),
                Location = new Point(2, stdHeight - 22)
            }, new Button()
            {
                Text = ">",
                Size = new Size(20, 20),
                Location = new Point(24, stdHeight - 22)
            });
            deleteButt.Click += (object sender, EventArgs e) => { deleteHandler(this); };
            infoButt.Click += (object sender, EventArgs e) => { MessageBox.Show($"Image infomation:\n\n\tPath: {image}\n\tName: {image.Substring(image.LastIndexOf('\\') + 1)}", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information); };
            selectButt.Click += (object sender, EventArgs e) => { selectionHandler(this); };
            moveButts.Item1.Click += (object sender, EventArgs e) => { moveHandler(this, false); };
            moveButts.Item2.Click += (object sender, EventArgs e) => { moveHandler(this, true);  };
            ctct.Controls.Add(imagePreview);
            ctct.Controls.Add(deleteButt);
            ctct.Controls.Add(infoButt);
            ctct.Controls.Add(moveButts.Item1);
            ctct.Controls.Add(moveButts.Item2);
            image = imagePath;
            this.Draw();
        }

        public void Draw()
        {
            imagePreview.Size = new Size(stdWidth - 4, stdHeight - 26);
            deleteButt.Location = new Point(stdWidth - 22, stdHeight - 22);
            infoButt.Location = new Point(stdWidth - 44, stdHeight - 22);
            selectButt.Location = new Point((stdWidth / 2) - 25, stdHeight - 22);
            moveButts.Item1.Top = stdHeight - 22;
            moveButts.Item2.Top = stdHeight - 22;
            ctct.Size = new Size(stdWidth, stdHeight);
            Size = new Size(stdWidth + 4, stdHeight + 4);
        }

        public void DestroyTile()
        {
            Parent.Controls.Remove(this);
        }
    }

    [Serializable]
    public class SavedDocument
    {
        public string frames;
        public string scenes;
        public string savelocation;
        public string filmname;
        public string projectname;

        public SavedDocument(Needle app)
        {
            savelocation = app.meta["savelocation"];
            filmname = app.meta["filmname"];
            projectname = app.meta["projectname"];
            scenes = "";
            frames = "";
            foreach (List<string> scene in app.film)
            {
                scenes += ToThreeLetter(scene.Count);
                foreach (string frameloc in scene) frames += ToThreeLetter(frameloc.Length) + frameloc;
            }
        }
        private static string ToThreeLetter(int val)
        {
            if (val > 999) return "000"; //ERROR
            string op = val.ToString();
            if (val < 100) op = "0" + op;
            if (val < 10) op = "0" + op;
            if (op.Length != 3) return "000";
            return op;
        }
        public Tuple<Dictionary<string, string>, List<List<string>>> ToDoc()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("savelocation", savelocation);
            dic.Add("filmname", filmname);
            dic.Add("projectname", projectname);
            List<string> fms = new List<string>();
            string framescp = frames;
            int ln, idex = 0;
            if (frames.Length > 3)
            {
                while (true)
                {
                    ln = int.Parse(framescp.Substring(0, 3));
                    fms.Add(framescp.Substring(3, ln));
                    if (framescp.Length == 3 + ln) break;
                    framescp = framescp.Substring(3 + ln);
                }
            }
            List<int> scs = new List<int>();
            for (int i = 0; i < scenes.Length; i+=3) scs.Add(int.Parse(scenes[i].ToString() + scenes[i + 1].ToString() + scenes[i + 2].ToString()));
            List<List<string>> mv = new List<List<string>>();
            foreach (int scl in scs)
            {
                mv.Add(fms.GetRange(idex, scl));
                idex += scl;
            }
            return new Tuple<Dictionary<string, string>, List<List<string>>>(dic, mv);
        }
    }
}