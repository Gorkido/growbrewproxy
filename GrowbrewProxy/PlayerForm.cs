using System;
using System.Drawing;
using System.Windows.Forms;

namespace GrowbrewProxy
{
    public partial class PlayerForm : Form
    {
        public PlayerForm()
        {
            InitializeComponent();
        }


        public struct ControlWithMetaData
        {
            public int netID;
            public int userID;
            public Control control;
        }

        public static int updatedHeight = 0;

        public void AddPlayerBtnToForm(string text, int netID, Point point)
        {
            if (IsHandleCreated)
            {
                if (playerBox.InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        Button btnAdd = new()
                        {
                            Location = point,
                            Text = text,
                            Name = netID.ToString(),
                            Width = 128,
                            Height = 32
                        };
                        btnAdd.Click += playerBtn_Click;
                        playerBox.Controls.Add(btnAdd);
                    }));
                }
                else
                {
                    Button btnAdd = new()
                    {
                        Location = point,
                        Text = text,
                        Name = netID.ToString(),
                        Width = 128,
                        Height = 32
                    };
                    btnAdd.Click += playerBtn_Click;
                    playerBox.Controls.Add(btnAdd);
                }
            }
        }

        public void LoadPlayerButtons() // reload
        {
            if (IsHandleCreated)
            {
                Invoke(new Action(() =>
                {
                    foreach (Player pl in MainForm.messageHandler.worldMap.players)
                    {
                        MainForm.messageHandler.worldMap.AddPlayerControlToBox(pl);
                    }
                }));
            }
        }
        private void PlayerForm_Load(object sender, EventArgs e)
        {
            foreach (Button btn in playerBox.Controls) // assume all controls are btns.
            {
                btn.Dispose();
            }

            playerBox.Controls.Clear();
            LoadPlayerButtons();
        }

        private void playerBtn_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int netID = int.Parse(btn.Name);
            World worldMap = MainForm.messageHandler.worldMap;
            Player pl = null;
            if (worldMap != null)
            {
                foreach (Player p in worldMap.players)
                {
                    if (p.netID == netID)
                    {
                        pl = p;
                        goto LABEL_RETRIEVED_PLAYER;
                    }
                }
            }

        LABEL_RETRIEVED_PLAYER:
            {
                if (pl == null)
                {
                    goto LABEL_FAILED_TO_RETRIEVE_PLAYER;
                }

                _ = MessageBox.Show("PLAYER INFOS:\n" +
                                "name/nickname: " + pl.name + "\n" +
                                "country: " + pl.country + "\n" +
                                "invisible: " + pl.invis.ToString() + "\n" +
                                "moderator power level: " + pl.mstate.ToString() + "\n" +
                                "isSuperModerator (higher than 0 = yes): " + pl.smstate.ToString() + "\n" +
                                "netID: " + netID.ToString() + "\n" +
                                "userID: " + pl.userID.ToString() + "\n" +
                                "X: " + pl.X + " Y: " + pl.Y);
                return;
            }

        LABEL_FAILED_TO_RETRIEVE_PLAYER:
            _ = MessageBox.Show("Could not retrieve player! The expected player left or an error occured.");

        }
        protected override void WndProc(ref Message m)
        {
            updatedHeight = Height;
            base.WndProc(ref m);
        }
    }
}
