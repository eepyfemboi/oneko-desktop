using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace oneko
{
    public class OnekoForm : Form
    {
        private Bitmap spriteSheet;
        private Timer timer;
        private PointF nekoPos = new PointF(100, 100);
        private Point mousePos;
        private float nekoSpeed = 10f;

        private Size spriteSize = new Size(32, 32);
        private int frame = 0;
        private string currentDirection = "idle";

        private int idleFrames = 0;
        private string idleAnimation = null;

        private int animationTick = 0;


        private readonly Dictionary<string, List<Point>> spriteMap = new()
        {
            ["idle"] = new() { new Point(-3, -3) },
            ["alert"] = new() { new Point(-7, -3) },
            ["N"] = new() { new Point(-1, -2), new Point(-1, -3) },
            ["NE"] = new() { new Point(0, -2), new Point(0, -3) },
            ["E"] = new() { new Point(-3, 0), new Point(-3, -1) },
            ["SE"] = new() { new Point(-5, -1), new Point(-5, -2) },
            ["S"] = new() { new Point(-6, -3), new Point(-7, -2) },
            ["SW"] = new() { new Point(-5, -3), new Point(-6, -1) },
            ["W"] = new() { new Point(-4, -2), new Point(-4, -3) },
            ["NW"] = new() { new Point(-1, 0), new Point(-1, -1) },
            ["tired"] = new() { new Point(-3, -2) },
            ["sleeping"] = new() { new Point(-2, 0), new Point(-2, -1) },
            ["scratchSelf"] = new() { new Point(-5, 0), new Point(-6, 0), new Point(-7, 0) }
        };

        public OnekoForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            Width = spriteSize.Width;
            Height = spriteSize.Height;
            StartPosition = FormStartPosition.Manual;

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.LimeGreen;
            TransparencyKey = Color.LimeGreen;

            spriteSheet = new Bitmap(new System.IO.MemoryStream(Properties.Resources.oneko));

            timer = new Timer { Interval = 1000 / 12 }; // ~12 FPS like the JS
            timer.Tick += UpdateNeko;
            timer.Start();

            Load += (_, _) => MakeClickThrough();
        }

        /*protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (!spriteMap.TryGetValue(currentDirection, out var frames))
                frames = spriteMap["idle"];

            var currentFrame = frames[frame % frames.Count];
            int col = Math.Abs(currentFrame.X);
            int row = Math.Abs(currentFrame.Y);

            Rectangle src = new Rectangle(col * spriteSize.Width, row * spriteSize.Height, spriteSize.Width, spriteSize.Height);
            g.DrawImage(spriteSheet, new Rectangle(0, 0, Width, Height), src, GraphicsUnit.Pixel);
        }*/

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (!spriteMap.TryGetValue(currentDirection, out var frames))
                frames = spriteMap["idle"];

            int effectiveFrame = frame;

            // Slow down animation for idle/sleeping/scratch
            if (currentDirection == "idle" || currentDirection == "sleeping" || currentDirection == "scratchSelf" || currentDirection == "tired")
                effectiveFrame = animationTick / 4; // Show one frame every ~4 ticks

            var current = frames[effectiveFrame % frames.Count];
            Rectangle src = new Rectangle(Math.Abs(current.X) * spriteSize.Width, Math.Abs(current.Y) * spriteSize.Height, spriteSize.Width, spriteSize.Height);
            g.DrawImage(spriteSheet, new Rectangle(0, 0, Width, Height), src, GraphicsUnit.Pixel);
        }



        private void UpdateNeko(object sender, EventArgs e)
        {
            mousePos = Cursor.Position;

            float dx = mousePos.X - nekoPos.X;
            float dy = mousePos.Y - nekoPos.Y;
            float distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance > 48f)
            {
                idleFrames = 0;
                idleAnimation = null;

                float nx = dx / distance;
                float ny = dy / distance;

                nekoPos.X += nx * nekoSpeed;
                nekoPos.Y += ny * nekoSpeed;

                Left = (int)nekoPos.X - Width / 2;
                Top = (int)nekoPos.Y - Height / 2;

                currentDirection = GetDirection(nx, ny);
            }
            else
            {
                idleFrames++;

                if (idleAnimation == null && idleFrames > 100 && new Random().Next(200) == 0)
                {
                    idleAnimation = new[] { "sleeping", "scratchSelf" }[new Random().Next(2)];
                    frame = 0;
                    animationTick = 0;

                }

                currentDirection = idleAnimation ?? "idle";
            }

            animationTick++;
            frame++;
            Invalidate();

        }

        private string GetDirection(float nx, float ny)
        {
            if (ny < -0.5f)
                return nx < -0.5f ? "NW" : nx > 0.5f ? "NE" : "N";
            if (ny > 0.5f)
                return nx < -0.5f ? "SW" : nx > 0.5f ? "SE" : "S";
            return nx < 0 ? "W" : "E";
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TRANSPARENT = 0x20;
                const int WS_EX_TOOLWINDOW = 0x80;
                const int WS_EX_NOACTIVATE = 0x8000000;

                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        private void MakeClickThrough()
        {
            int wl = GetWindowLong(Handle, GWL_EXSTYLE);
            wl |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            SetWindowLong(Handle, GWL_EXSTYLE, wl);
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    }
}
