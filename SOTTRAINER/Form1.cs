using swed32;
using System.Numerics;
using System.Runtime.InteropServices;



namespace SOTTRAINER
{
    public partial class Form1 : Form
    {
        #region hotkey import

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);

        #endregion

        #region global variables

        swed swed = new swed();
        List<entity> allEntities = new List<entity>();
        IntPtr moduleBase;
        Font largeFont = new Font("Arial", 10);
        int selectedIndex = 0;
        #endregion

        #region mouse stuff 

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        #endregion


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            swed.GetProcess("South Park - The Stick of Truth");
            moduleBase = swed.GetModuleBase(".exe");
            Thread colorThread = new Thread(ColorLoop) { IsBackground = true }; 
            Thread thread = new Thread(Main) { IsBackground = true };
            thread.Start();
            colorThread.Start();
        }
        Panel CreateEntityPanel(int index, string content)
        {
            Panel myPanel = new Panel();
            myPanel.Visible = true;
            myPanel.Name = index.ToString();
            myPanel.BackColor = Color.Gray;
            Label label = new Label();
            label.Text = content;
            label.Font = largeFont;
            myPanel.Controls.Add(label);
            myPanel.Size = new Size(200, 33);
            myPanel.Click += new EventHandler(myPanel_clicked);
            return myPanel;
        }
        void TelePortToPos(Vector3 pos,int timer)
        {
            var localPlayer = swed.ReadPointer(moduleBase, 0x0B579B0);
            var bytePos = BitConverter.GetBytes(pos.X).Concat(BitConverter.GetBytes(pos.Y)).Concat(BitConverter.GetBytes(pos.Z)).ToArray();

            for (int i = 0; i < 25; i++)
            {
                swed.WriteBytes(localPlayer, 0x12C, bytePos);
                Thread.Sleep(timer);
            }
        }
        void LoadEntity(entity entity)
        {

            label5.Text = "0x" + entity.baseAddress.ToString("X");
            label2.Text = entity.name;
            textBox1.Text = entity.currentHealth.ToString();
            textBox2.Text = entity.maxHealth.ToString();
            label7.Text = entity.pos.ToString();

        }
        void SetChanges(entity entity)
        {
            var newCurrentHealth = float.Parse(textBox1.Text);
            var newMaxHealth = float.Parse(textBox2.Text);

            var healthComponent = swed.ReadPointer(entity.baseAddress, 0x14);

            // health component seems to be at offset 0x14 or 0x84

            swed.WriteBytes(healthComponent, 0x18, BitConverter.GetBytes(newCurrentHealth));
            swed.WriteBytes(healthComponent, 0x1C, BitConverter.GetBytes(newMaxHealth));

            healthComponent = swed.ReadPointer(entity.baseAddress, 0x84);

            swed.WriteBytes(healthComponent, 0x18, BitConverter.GetBytes(newCurrentHealth));
            swed.WriteBytes(healthComponent, 0x1C, BitConverter.GetBytes(newMaxHealth));

        }
        void UpdateEntities()
        {
            allEntities.Clear();

            var entityList = swed.ReadPointer(moduleBase, 0x1B71110);
            

            for (int i = 0;i < 50; i++)
            {
                var currentEntity = swed.ReadPointer(entityList, 0x4 * i);

                var gameObject = swed.ReadPointer(currentEntity, 0xC);
                var healthComponent = swed.ReadPointer(currentEntity, 0x14);
                var bytePos = swed.ReadBytes(gameObject, 0x10, 12);



                var ent = new entity
                {
                    baseAddress = currentEntity,
                    currentHealth = BitConverter.ToSingle(swed.ReadBytes(healthComponent, 0x18, 4), 0),
                    maxHealth = BitConverter.ToSingle(swed.ReadBytes(healthComponent, 0x1C, 4), 0),
                    name = System.Text.Encoding.UTF8.GetString(swed.ReadBytes(gameObject,0x30,19)).Replace("\0",String.Empty),

                    pos = new Vector3 {
                        X = BitConverter.ToSingle(bytePos,0),
                        Y = BitConverter.ToSingle(bytePos, 4),
                        Z = BitConverter.ToSingle(bytePos, 8)
                    }
                };

                if (ent.name != "")
                    allEntities.Add(ent);


                if (ent.maxHealth == 0)
                {
                    healthComponent = swed.ReadPointer(currentEntity, 0x84);
                    ent.currentHealth = BitConverter.ToSingle(swed.ReadBytes(healthComponent, 0x18, 4), 0);
                    ent.maxHealth = BitConverter.ToSingle(swed.ReadBytes(healthComponent, 0x1C, 4), 0);

                }

            }

        }
        void UpdatePanels()
        {
            UpdateEntities();
            flowLayoutPanel1.Controls.Clear();
            bool inCombat = false;
            for (int i = 0; i < allEntities.Count; i++)
            {
                var myPanel = CreateEntityPanel(i, allEntities[i].name);
                if (inCombat)
                {
                    myPanel.BackColor = Color.Red;
                }
                if (allEntities[i].name.Contains("combat"))
                {
                    myPanel.BackColor = Color.Green;
                    inCombat = true;

                }
                if (this.flowLayoutPanel1.InvokeRequired)
                {
                    this.flowLayoutPanel1.Invoke((MethodInvoker)delegate
                    {
                        this.flowLayoutPanel1.Controls.Add(myPanel);   
                    });
                }
                else
                {
                    this.flowLayoutPanel1.Controls.Add(myPanel);
                }
            }
        }
        void SetChanges()
        {
            // do not leave text fields empty! I am lazy and fuck you 
            var entity = allEntities[0];
            var newCurrentHealth = float.Parse(textBox4.Text);
            var newMaxHealth = float.Parse(textBox3.Text);

            var newCurrentStamina = float.Parse(textBox6.Text);
            var newMaxStamina = float.Parse(textBox5.Text);

            var newCurrency = int.Parse(textBox7.Text);
            Vector3 destination = new Vector3();

            if (textBox10.Text != "0" && textBox9.Text != "0" && textBox8.Text != "0" && float.TryParse(textBox10.Text, out destination.Z) && float.TryParse(textBox9.Text, out destination.Y) && float.TryParse(textBox8.Text, out destination.X))
            {
                TelePortToPos(destination, 100);
            }



            var healthComponent = swed.ReadPointer(entity.baseAddress, 0x14);

            // health component seems to be at offset 0x14 or 0x84 or 0x64 >:/

            swed.WriteBytes(healthComponent, 0x18, BitConverter.GetBytes(newCurrentHealth));
            swed.WriteBytes(healthComponent, 0x1C, BitConverter.GetBytes(newMaxHealth));
            swed.WriteBytes(healthComponent, 0x19C, BitConverter.GetBytes(newCurrentStamina));
            swed.WriteBytes(healthComponent, 0x1A0, BitConverter.GetBytes(newMaxStamina));
            swed.WriteBytes(healthComponent, 0x218, BitConverter.GetBytes(newCurrency));

            healthComponent = swed.ReadPointer(entity.baseAddress, 0x64);

            swed.WriteBytes(healthComponent, 0x18, BitConverter.GetBytes(newCurrentHealth));
            swed.WriteBytes(healthComponent, 0x1C, BitConverter.GetBytes(newMaxHealth));
            swed.WriteBytes(healthComponent, 0x19C, BitConverter.GetBytes(newCurrentStamina));
            swed.WriteBytes(healthComponent, 0x1A0, BitConverter.GetBytes(newMaxStamina));
            swed.WriteBytes(healthComponent, 0x218, BitConverter.GetBytes(newCurrency));


            healthComponent = swed.ReadPointer(entity.baseAddress, 0x84);

            swed.WriteBytes(healthComponent, 0x18, BitConverter.GetBytes(newCurrentHealth));
            swed.WriteBytes(healthComponent, 0x1C, BitConverter.GetBytes(newMaxHealth));
            swed.WriteBytes(healthComponent, 0x19C, BitConverter.GetBytes(newCurrentStamina));
            swed.WriteBytes(healthComponent, 0x1A0, BitConverter.GetBytes(newMaxStamina));
            swed.WriteBytes(healthComponent, 0x218, BitConverter.GetBytes(newCurrency));

        }
        void LoadPlayer()
        {
            var ent = allEntities[0];

            label11.Text = "0x" + ent.baseAddress.ToString("X");
            textBox4.Text = ent.currentHealth.ToString();
            textBox3.Text = ent.maxHealth.ToString();
            label9.Text = ent.pos.ToString();

            textBox6.Text = "99";
            textBox5.Text = "99";

            textBox10.Text = ent.pos.Z.ToString();
            textBox9.Text = ent.pos.Y.ToString();
            textBox8.Text = ent.pos.X.ToString();

            textBox7.Text = "9999";


        }
        void ColorLoop()
        {
            for (int r = 0; r <= 255; r++)
            {
                label6.ForeColor = Color.FromArgb(r, 0, 0);
                Thread.Sleep(1);
            }
        }
        void Main()
        {

            while (true)
            {
                // UpdateEntities();

                var localPlayer = swed.ReadPointer(moduleBase, 0x0B579B0);
                var bytePos = swed.ReadBytes(localPlayer, 0x12C, 12);

                var pos = new Vector3
                {
                    X = BitConverter.ToSingle(bytePos, 0),
                    Y = BitConverter.ToSingle(bytePos, 4),
                    Z = BitConverter.ToSingle(bytePos, 8)
                };

                #region teleport on keys

                if (GetAsyncKeyState(Keys.Down) < 0)
                {
                    pos.Z += 1f;
                    TelePortToPos(pos, 5);
                }
                if (GetAsyncKeyState(Keys.Up) < 0)
                {
                    pos.Z -= 1f;
                    TelePortToPos(pos, 5);
                }
                if (GetAsyncKeyState(Keys.Left) < 0)
                {
                    pos.X -= 1f;
                    TelePortToPos(pos, 5);
                }
                if (GetAsyncKeyState(Keys.Right) < 0)
                {
                    pos.X += 1f;
                    TelePortToPos(pos, 5);
                }
                #endregion
                Thread.Sleep(50);
            }



        }


        #region tie up form clicks and shit 

        private void myPanel_clicked(object? sender, EventArgs e)
        {
            foreach (Panel panel in flowLayoutPanel1.Controls)
            {
                if (panel.BackColor != Color.Red && panel.BackColor != Color.Green)
                    panel.BackColor = Color.Gray;
            }
            Panel? myPanel = sender as Panel;
            if (myPanel != null)
            {
                myPanel.BackColor = Color.DimGray;
                LoadEntity(allEntities[int.Parse(myPanel.Name)]);
                selectedIndex = int.Parse(myPanel.Name);
            }
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            UpdatePanels();
            LoadPlayer();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (selectedIndex < allEntities.Count)
            {
                SetChanges(allEntities[selectedIndex]);
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            TelePortToPos(allEntities[selectedIndex].pos,100);
        }
        private void button6_Click(object sender, EventArgs e)
        {
            SetChanges();
        }
        #endregion
    }
}