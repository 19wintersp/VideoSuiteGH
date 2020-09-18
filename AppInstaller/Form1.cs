using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using IWshRuntimeLibrary;

namespace AppInstaller
{
    public partial class Installer : Form
    {
        private string installLoc = "";
        public Installer()
        {
            InitializeComponent();
            Text = "VideoSuite Installer";
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(400, 375 - 45);
            Size = new Size(400, 375 - 45);
            MaximumSize = new Size(400, 375 - 45);
            BackColor = Color.White;
            Font = new Font("Segoe UI Emoji", 10);
            ContainerControl pg1 = new ContainerControl()
            {
                Size = new Size(380, 195),
                Location = new Point(10, 170)
            }, pg2 = new ContainerControl()
            {
                Size = new Size(380, 195),
                Location = new Point(10, 170),
                Visible = false
            };
            TextBox folderName = new TextBox()
            {
                PlaceholderText = "Required field",
                Size = new Size(380, 25),
                Location = new Point(0, 25)
            };
            Button next = new Button()
            {
                Text = "Next",
                Size = new Size(100, 25),
                Location = new Point(280, 170)
            }, canx = new Button()
            {
                Text = "Cancel",
                Size = new Size(100, 25),
                Location = new Point(170, 170)
            };
            CheckBox cdks = new CheckBox()
            {
                Text = "Create Desktop shortcut(s)",
                Checked = true,
                Size = new Size(380, 20),
                Location = new Point(0, 115)
            }, sdds = new CheckBox()
            {
                Text = "Self-destruct",
                Checked = false,
                Size = new Size(380, 20),
                Location = new Point(0, 140)
            };
            next.Click += (object sender, EventArgs e) => {
                if (installLoc == "") return;
                pg1.Visible = false;
                pg2.Visible = true;
                BeginDownload(installLoc);
            };
            canx.Click += (object sender, EventArgs e) =>
            {
                this.Dispose();
                Application.Exit();
            };
            folderName.TextChanged += (object semder, EventArgs e) => {
                if (folderName.Text == "") installLoc = "";
                else installLoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + folderName.Text;
                pg1.Controls[pg1.Controls.Count - 1].Text = (installLoc == "" ? "Please choose a folder name." : installLoc);
            };
            pg1.Controls.Add(folderName);
            pg1.Controls.Add(canx);
            pg1.Controls.Add(next);
            pg1.Controls.Add(cdks);
            pg1.Controls.Add(sdds);
            pg1.Controls.Add(new Label()
            {
                Text = "Advanced options:",
                Location = new Point(5, 90),
                Size = new Size(375, 20)
            });
            pg1.Controls.Add(new Label()
            {
                Text = "Installation folder:",
                Location = new Point(5, 0),
                Size = new Size(375, 20)
            });
            pg1.Controls.Add(new Label()
            {
                Text = "Please choose a folder name.",
                Size = new Size(375, 20),
                Location = new Point(5, 55)
            });
            Controls.Add(pg1);
            Controls.Add(pg2);
            Controls.Add(new Control()
            {
                Text = "",
                BackgroundImage = Image.FromFile("videosuite.png"),
                BackgroundImageLayout = ImageLayout.Stretch,
                Size = new Size(380, 100),
                Location = new Point(10, 10)
            });
            Controls.Add(new Label()
            {
                Text = "VideoSuite Installer",
                Location = new Point(10, 120),
                Size = new Size(380, 40),
                Font = new Font("Segoe UI Emoji", 20)
            });
            folderName.Text = "VideoSuite";
        }

        private void BeginDownload(string targetFolder)
        {

        }

        private void DesktopShortcut(string name, string target)
        {
            object shDesktop = (object)"Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + "\\" + name + ".lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.TargetPath = target;
            shortcut.Save();
        }
    }

    public partial class Form0 : Form
    {

        private readonly App keyframer = new App() {
            dirname = "KeyframerApp",
            name = "Keyframer",
            version = "I0.1",
            installable = false,
            url = "https://github.com/antarcticappstudio/VideoSuite/Keyframer/bin/x86/release.zip"
        };
        private readonly App stitcher = new App()
        {
            dirname = "StitchingNeedle",
            name = "Stitcher",
            version = "A1.5",
            url = "https://github.com/antarcticappstudio/VideoSuite/Stitcher/bin/x86/release.zip"
        };
        private readonly App transition = new App()
        {
            dirname = "Editor",
            name = "Transition",
            version = "I0.1",
            url = "https://github.com/antarcticapptudio/VideoSuite/Transition/bin/x86/release.zip",
            installable = false
        };

        public Form0()
        {
            //InitializeComponent();
            this.Text = "VideoSuite Installer";
            this.Size = new Size(220, 220);
            this.ControlBox = false;
            this.ShowIcon = false;
            this.Controls.Add(new Label()
            {
                Text = "Install VideoSuite:",
                Location = new Point(5, 5),
                Size = new Size(190, 20)
            });
            keyframer.paint(this, new Point(20, 25), new Size(190, 18));
            stitcher.paint(this, new Point(20, 43), new Size(190, 18));
            transition.paint(this, new Point(20, 61), new Size(190, 18));
            this.Controls.Add(new TextBox()
            {
                PlaceholderText = "Username",
                Location = new Point(5, 85),
                Size = new Size(190, 25)
            }) ;
            this.Controls.Add(new Label()
            {
                Text = "Please enter your user name",
                ForeColor = Color.Red,
                Visible = false,
                Location = new Point(15, 110),
                Size = new Size(190, 20)
            });
            this.Controls.Add(new Label()
            {
                Text = "License: GNUGPLv3",
                Location = new Point(5, 130),
                Size = new Size(190, 20)
            });
            Button sub = new Button() { Text = "Install", Location = new Point(135, 150), Size = new Size(60, 22) };
            sub.Click += new EventHandler(RequestDownload);
            this.Controls.Add(sub);
            Button canx = new Button() { Text = "Cancel", Location = new Point(70, 150), Size = new Size(60, 22) };
            canx.Click += new EventHandler(Exit);
            this.Controls.Add(canx);
        }

        private void RequestDownload(object sender, EventArgs e)
        {
            Form sdfm = ((Button)sender).FindForm();
            if (!System.IO.Directory.Exists("C:\\Users\\" + ((TextBox)sdfm.Controls[4]).Text))
            {
                this.Controls[5].Visible = true;
                return;
            } else
            {
                this.Controls[5].Visible = false;
                string user = ((TextBox)sdfm.Controls[4]).Text;
                int status = 0;
                if (((CheckBox)sdfm.Controls[1]).Checked)
                {
                    status = keyframer.install(user);
                    App.alertStatus(keyframer, status);
                }
                if (((CheckBox)sdfm.Controls[2]).Checked)
                {
                    stitcher.install(user);
                    App.alertStatus(keyframer, status);
                }
                if (((CheckBox)sdfm.Controls[3]).Checked)
                {
                    transition.install(user);
                    App.alertStatus(keyframer, status);
                }
            }
            Exit(sender, e);
        }

        private void Exit(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    public class App
    {
        public string name;
        public string version;
        public string url;
        public string dirname;
        public bool installable = true;
        public string target = "x86";
        private List<string> errors;

        public void paint(Form fm, Point loc, Size size)
        {
            fm.Controls.Add(new CheckBox()
            {
                Text = this.name + " (" + this.target + ")",
                Checked = this.installable,
                Enabled = this.installable,
                Location = loc,
                Size = size
            });
        }

        public int install(string user)
        {
            if (System.IO.Directory.Exists("C:\\Users\\" + user + "\\Documents") && System.IO.Directory.Exists("C:\\Users\\" + user + "\\Desktop"))
            {
                try
                {
                    using (System.Net.WebClient client = new System.Net.WebClient())
                    {
                        client.DownloadFile(url, "C:\\Users\\" + user + "\\Documents\\tmp\\bin" + target + "-" + dirname + ".zip");
                    }
                } catch(Exception e)
                {
                    errors.Add(e.ToString());
                    return 2; //Download error
                }
                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory("C:\\Users\\" + user + "\\Documents\\tmp\\bin" + target + "-" + dirname + ".zip", "C:\\Users\\" + user + "\\Documents\\VideoSuite\\" + dirname);
                } catch(Exception e)
                {
                    errors.Add(e.ToString());
                    return 3; //Decompression error
                }
                try
                {
                    System.IO.File.Move("C:\\Users\\" + user + "\\Documents\\VideoSuite\\" + dirname + "\\launch.bat", "C:\\Users\\" + user + "\\Documents\\VideoSuite\\" + name + ".bat");
                    System.IO.File.Move("C:\\Users\\" + user + "\\Documents\\VideoSuite\\" + dirname + "\\desktoplaunch.bat", "C:\\Users\\" + user + "\\Desktop\\" + name + ".bat");
                } catch(Exception e)
                {
                    errors.Add(e.ToString());
                    return 4; //File movement error
                }
                return 0; //Success
            } else
            {
                return 1; //User not found
            }
        }

        public static void alertStatus(App app, int status)
        {
            if (status == 0) MessageBox.Show(app.name + " installed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (status == 1) MessageBox.Show("Your user documents/desktop folder could not be found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (status == 2) MessageBox.Show("There was an error whilst downloading " + app.name + ".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (status == 3) MessageBox.Show("There was an error whilst unzipping " + app.name + "'s download.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (status == 4) MessageBox.Show("There was an error whilst creating shortcuts for " + app.name + ".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
