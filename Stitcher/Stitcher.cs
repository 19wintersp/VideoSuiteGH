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

namespace Stitcher
{
    public partial class Stitcher : Form
    {
        private MenuStrip menuStrip1;
        private ToolStripItem tsdi1;
        private ContainerControl refbox;
        private DocumentMeta openDoc;
        private Document doc;
        private Form cbx;
        private List<string> refs = new List<string>();
        private List<Label> refcts = new List<Label>();
        private static readonly string stitcherVersion = "[ALPHA 1.7]";
        private VideoCaptureDevice vsource;
        private List<List<string>> scenes = new List<List<string>>();

        public Stitcher()
        {
            InitializeComponent();
            this.ResizeEnd += new EventHandler(RedrawUi);
            this.InitializeUi();
            this.DrawUi();
        }
        public Stitcher(string pathToOpen)
        {
            InitializeComponent();
            this.ResizeEnd += new EventHandler(RedrawUi);
            this.InitializeUi();
            this.DrawUi();
            this.Open(pathToOpen);
        }

        private void InitializeUi()
        {
            /*// WINDOW/APP //*/
            this.Text = "Stitcher " + stitcherVersion;
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
            filemenu.DropDownItems.Add("Quit", (Image)null, new EventHandler(QuitApp));
            ToolStripMenuItem framemenu = new ToolStripMenuItem("Import Frame...");
            framemenu.DropDownItems.Add("From Camera", (Image)null, new EventHandler(CameraFrame));
            framemenu.DropDownItems.Add("From File", (Image)null, new EventHandler(FileFrame));
            videomenu.DropDownItems.Add(framemenu);
            videomenu.DropDownItems.Add("New Scene", (Image)null, new EventHandler(NewScene));
            videomenu.DropDownItems.Add("New Frame");
            //videomenu.DropDownItems.Add("New Frame", (Image)null, new EventHandler(NewFrame));
            videomenu.DropDownItems.Add(new ToolStripSeparator());
            videomenu.DropDownItems.Add("Export", (Image)null, new EventHandler(ExportVideo));
            viewmenu.DropDownItems.Add("Reload", (Image)null, new EventHandler(UpdateEdit));
            viewmenu.DropDownItems.Add("Refresh view", (Image)null, new EventHandler(RedrawUi));
            menuStrip1.Items.Add(filemenu);
            menuStrip1.Items.Add(viewmenu);
            menuStrip1.Items.Add(videomenu);
            this.MainMenuStrip = menuStrip1;
            this.Controls.Add(menuStrip1);
            /*// SETTINGS //*/

            /*// REFERENCES //*/

            /*// VIEWPORT //*/

            /*// TIMELINE //*/

            /*// SCENES //*/
        }
        private void RedrawUi(object sender, EventArgs e) { DrawUi(); }
        private void DrawUi()
        {
            //Hang on a sec
        }
        private void UpdateEdit(object sender, EventArgs e) { UpdateEdit(this.doc); }
        private void UpdateEdit(Document doc)
        {
            //Update editing UI
        }
        private void NewVid(object sender, EventArgs e)
        {
            if (MessageBox.Show("Any unsaved changes will be lost. Continue?", "New Project", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) != DialogResult.Yes)
            {
                return;
            }
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
        private void SaveCnm(object sender, EventArgs e) {
            if (openDoc == null)
            {
                MessageBox.Show("No open document to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (openDoc.savelocation != null && tsdi1.Enabled) {
                Save(System.IO.File.Open(openDoc.savelocation, System.IO.FileMode.Open), openDoc.savelocation);
            } else {
                MessageBox.Show("The Document could not be found. Please save the document with a name first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void SaveAs(object sender, EventArgs e)
        {
            if (openDoc == null)
            {
                MessageBox.Show("No open document to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SaveFileDialog sfdialog = new SaveFileDialog();
            sfdialog.Filter = "Stitcher files (*.ndls)|*.ndls;*.stitchdoc;*.stcmf";
            if (sfdialog.ShowDialog() == DialogResult.OK)
            {
                openDoc.savelocation = sfdialog.FileName;
                System.IO.FileStream fs = System.IO.File.Open(sfdialog.FileName, System.IO.FileMode.OpenOrCreate);
                Save(fs, sfdialog.FileName);
            }
        }
        private void Save(System.IO.FileStream sfstream, string path)
        {
            this.doc.frames = "";
            this.doc.scenes = "";
            foreach (List<string> scene in scenes)
            {
                string ssl = scene.Count.ToString();
                if (ssl.Length > 3)
                {
                    MessageBox.Show("Too many frames in scene. Try splitting the scene so that there are less than 1000 frames.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (ssl.Length < 10) ssl = "00" + ssl;
                else if (ssl.Length < 100) ssl = "0" + ssl;
                this.doc.scenes += ssl;
                foreach (string frame in scene)
                {
                    string sf = frame.Length.ToString();
                    if (sf.Length > 3)
                    {
                        MessageBox.Show("File path is too long: paths are limited to a length of 999 characters. Please try moving your file closer to the root directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (frame.Length < 10)
                        sf = "00" + sf;
                    else if (frame.Length < 100)
                        sf = "0" + sf;
                    this.doc.frames += sf + frame;
                }
            }

            System.IO.FileStream sfx = System.IO.File.Open(path+".meta", System.IO.FileMode.OpenOrCreate);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            binf.Serialize(sfstream, new SavedDocument(this.doc));
            binf.Serialize(sfx, this.doc.metadata);
            //binf.Serialize(sfstream, new TestClass());
            //binf.Serialize(sfx, new TestClass());
            sfstream.Close();
            sfx.Close();
            tsdi1.Enabled = true;
        }
        private void OpenFile(object sender, EventArgs e){ OpenFile(); }
        private void OpenFile()
        {
            OpenFileDialog ofdialog = new OpenFileDialog();
            ofdialog.Filter = "Stitcher files (*.ndls)|*.ndls;*.stitchdoc;*.stcmf";
            if (ofdialog.ShowDialog() == DialogResult.OK)
            {
                Open(ofdialog.FileName);
            }
        }
        private void Open(string path)
        {
            System.IO.FileStream fs = System.IO.File.Open(path, System.IO.FileMode.Open);
            System.IO.FileStream mfs = System.IO.File.Open(path + ".meta", System.IO.FileMode.Open);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            doc = ((SavedDocument)binf.Deserialize(fs)).ToDoc();
            openDoc = ((DocumentMeta)binf.Deserialize(mfs));
            this.Text = openDoc.filmName + " - Stitcher " + stitcherVersion;
            fs.Close();
            mfs.Close();

            scenes = new List<List<string>>();
            string scenelength = "";
            string sfrm = this.doc.frames;
            List<string> cs = new List<string>();
            foreach (char scenelengthdigit in this.doc.scenes.ToArray<char>())
            {
                scenelength = scenelength + scenelengthdigit.ToString();
                if (scenelength.Length == 3)
                {
                    int finsc = int.Parse(scenelength);
                    scenelength = "";
                    for (int i = 0; i < finsc; i++)
                    {
                        int pathl = int.Parse(sfrm.Substring(0, 3));
                        cs.Add(sfrm.Substring(3, pathl));
                        sfrm = sfrm.Substring(3 + pathl);
                    }
                    scenes.Add(cs);
                    cs = new List<string>();
                }
            }

            UpdateEdit(this.doc);
        }
        private void NewFile(object sender, EventArgs e) {
            Button cbt = ((Button)sender);
            Form pfm = cbt.FindForm();
            //MessageBox.Show(pfm.Controls[0].Text);
            this.doc = new Document(((TextBox)pfm.Controls[0]).Text, ((TextBox)pfm.Controls[1]).Text);
            this.openDoc = this.doc.metadata;
            scenes = new List<List<string>>();
            UpdateEdit(this.doc);
            DrawUi();
            pfm.Close();
        }
        private void AddToSources(string path)
        {
            refs.Add(path);
            string fn = path.Split(new char[] { '\\' })[path.Split(new char[] { '\\' }).Length - 1];
            Label nit = new Label() {
                Text = fn + ": " + path,
                Location = new Point(3, refcts.Count * 20),
                Size = new Size(refbox.Width - 10, 20)
            };
            refcts.Add(nit);
            refbox.Controls.Add(nit);
        }
        private void NewScene(object sender, EventArgs e) { NewScene(); }
        private void NewScene()
        {
            if (openDoc == null)
            {
                MessageBox.Show("No open document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MessageBox.Show("Scenes will become supported in a later update.");
            //ADD SCENES!
        }
        private void NewFrame(object sender, EventArgs e)
        {
            if (openDoc == null)
            {
                MessageBox.Show("No open document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult chosenopt = MessageBox.Show("Would you like to create a new image from the camera?", "New Frame", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (chosenopt == DialogResult.Yes)
            {
                CameraFrame();
            } else if (chosenopt == DialogResult.No)
            {
                FileFrame();
            }
        }
        private void CameraFrame(object sender, EventArgs e) { CameraFrame(); }
        private void CameraFrame()
        {
            if (openDoc == null)
            {
                MessageBox.Show("No open document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            vsource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            vsource.NewFrame += new AForge.Video.NewFrameEventHandler(CamNewFrame);
            vsource.Start();
        }
        private void CamNewFrame(object sender, AForge.Video.NewFrameEventArgs e)
        {
            vsource.SignalToStop();
            Bitmap takenphoto = e.Frame;
            string cdcidfpn = System.IO.Directory.GetCurrentDirectory() + "\\TakenImages\\" + openDoc.docName;
            if (!System.IO.Directory.Exists(cdcidfpn)) System.IO.Directory.CreateDirectory(cdcidfpn);
            string bmpfn = cdcidfpn + "\\" + (System.IO.Directory.GetFiles(cdcidfpn).Length + 1).ToString() + ".jpeg";
            if (System.IO.File.Exists(bmpfn)) return;
            takenphoto.Save(bmpfn, System.Drawing.Imaging.ImageFormat.Jpeg);
            AddToSources(bmpfn);
        }
        private void FileFrame(object sender, EventArgs e) { FileFrame(); }
        private void FileFrame()
        {
            if (openDoc == null)
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
            if (MessageBox.Show("Your project will be saved before export. Continue?", "Export", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.Cancel) return;
            SaveCnm(new object(), new EventArgs());
            if (openDoc == null)
            {
                MessageBox.Show("No open document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
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
            string selloc = exdialog.FileName;
            VideoCodec usedcodec = VideoCodec.MPEG4;
            if (selloc.EndsWith(".mp4")) usedcodec = VideoCodec.MPEG4;
            else if (selloc.EndsWith(".mp2")) usedcodec = VideoCodec.MPEG2;
            else if (selloc.EndsWith(".flv")) usedcodec = VideoCodec.FLV1;
            else if (selloc.EndsWith(".raw")) usedcodec = VideoCodec.Raw;
            else selloc += ".mp4";
            VideoFileWriter exporter = new VideoFileWriter();
            exporter.Open(selloc, Int32.Parse(res[0]), Int32.Parse(res[1]), Int32.Parse(expfm.Controls[1].Text), usedcodec);
            string frame, frames = doc.frames;
            bool errorOccurred = false;
            while (true)
            {
                try
                {
                    frame = frames.Substring(3, Int32.Parse( frames.Substring(0, 3)));
                }
                catch (ArgumentOutOfRangeException err)
                {
                    MessageBox.Show("Video Export failed: frames could not be found. Message:\n" + err.ToString(), "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    errorOccurred = true;
                    break;
                }
                try
                {
                    exporter.WriteVideoFrame(new Bitmap(frame));
                } catch (Exception err)
                {
                    MessageBox.Show("An error occurred whilst exporting your video. Message:\n" + err.ToString(), "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    errorOccurred = true;
                    break;
                }
                frames = frames.Substring(Int32.Parse(frames.Substring(0, 3)) + 3);
                if (frames.Length == 0) break;
            }
            if (errorOccurred) return;
            exporter.Close();
            MessageBox.Show("Video exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ProjectSettings(object sender, EventArgs e) { ProjectSettings(); }
        private void ProjectSettings()
        {
            if (openDoc == null)
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
            openDoc.docName = originfm.Controls[0].Text;
            openDoc.filmName = originfm.Controls[1].Text;
            CloseCBX(sender, e);
            if (openDoc.savelocation != null) SaveCnm(sender, e); /*Autosave*/
            this.Text = openDoc.filmName + " - Stitcher " + stitcherVersion;
        }
        private void CloseCBX(object sender, EventArgs e) { cbx.Close(); }
        private void QuitApp(object sender, EventArgs e) { Application.Exit(); }
        private void QuitApp() { Application.Exit(); }
    }

    [Serializable]
    public class DocumentMeta
    {
        public string savelocation;
        public string docName="Untitled Project";
        public string filmName = "Unnamed Film";
        
        public DocumentMeta(string adocName, string afilmName)
        {
            docName = adocName;
            filmName = afilmName;
        }
    }
    
    public class Document
    {
        public DocumentMeta metadata;
        public string placeholder;
        public string frames = "";
        public string scenes = "";
        public Document(string docName, string filmName)
        {
            metadata = new DocumentMeta(docName, filmName);
        }
    }

    [Serializable]
    public class SavedDocument
    {
        //All Document-class variables
        public string placeholder;
        public string frames;
        public string scenes;

        public SavedDocument(Document doc)
        {
            placeholder = doc.placeholder;
            frames = doc.frames;
            scenes = doc.scenes;
        }
        public Document ToDoc()
        {
            Document doc = new Document("", "");
            doc.placeholder = placeholder;
            doc.frames = frames;
            doc.scenes = scenes;
            return doc;
        }
    }

    [Serializable]
    public class TestClass
    {
        public int testvar;
        public TestClass()
        {
            testvar = 10;
        }
    }
}