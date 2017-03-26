using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string filename = "D:/_DEV/pic/test8x8.png";
            LoadImage(filename);
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (this.m_BgTexture != null)
            {
//                e.Graphics.DrawImage(this.m_BgTexture, this.ClientRectangle);
            }

            if (this.m_bUpdate && (this.m_PolygonList != null))
            {
                foreach (var i in this.m_PolygonList)
                {
                    VertexData vertex0 = this.m_VertexList[i.m_Index0];
                    VertexData vertex1 = this.m_VertexList[i.m_Index1];
                    VertexData vertex2 = this.m_VertexList[i.m_Index2];

                    vertex0.m_Position.X *= this.ClientRectangle.Width;
                    vertex0.m_Position.Y *= this.ClientRectangle.Height;
                    vertex1.m_Position.X *= this.ClientRectangle.Width;
                    vertex1.m_Position.Y *= this.ClientRectangle.Height;
                    vertex2.m_Position.X *= this.ClientRectangle.Width;
                    vertex2.m_Position.Y *= this.ClientRectangle.Height;

                    LinearGradientBrush brush01 = new LinearGradientBrush(vertex0.m_Position, vertex1.m_Position, vertex0.m_Color, vertex1.m_Color);
                    LinearGradientBrush brush12 = new LinearGradientBrush(vertex1.m_Position, vertex2.m_Position, vertex1.m_Color, vertex2.m_Color);
                    LinearGradientBrush brush20 = new LinearGradientBrush(vertex2.m_Position, vertex0.m_Position, vertex2.m_Color, vertex0.m_Color);

                    Pen pen01 = new Pen(brush01);
                    Pen pen12 = new Pen(brush12);
                    Pen pen20 = new Pen(brush20);

                    e.Graphics.DrawLine(pen01, vertex0.m_Position, vertex1.m_Position);
                    e.Graphics.DrawLine(pen12, vertex1.m_Position, vertex2.m_Position);
                    e.Graphics.DrawLine(pen20, vertex2.m_Position, vertex0.m_Position);
                }

                m_bUpdate = false;
            }

            //e.Graphics.Dispose();
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            string[] drags = (string[])(e.Data.GetData(DataFormats.FileDrop));
            foreach (string i in drags)
            {
                if (!System.IO.File.Exists(i))
                {
                    // ファイル以外であればイベント・ハンドラを抜ける
                    return;
                }
            }

            e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] drags = (string[])( e.Data.GetData(DataFormats.FileDrop) );
            foreach ( var i in drags)
            {
                if ( LoadImage(i) )
                {
                    break;
                }
            }
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            this.Invalidate();

            m_bUpdate = true;
        }

        private bool LoadImage(string filename)
        {
            Image image = null;

            try
            {
                image = Image.FromFile(filename);
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine(filename + "の読み込みに失敗");
                return false;
            }

            if (image == null)
            {
                return false;
            }

            Console.WriteLine(filename);

            int width = Math.Min(image.Width, 64);
            int height = Math.Min(image.Height, 64);
            Bitmap bitmap = new Bitmap(image, width, height);

            image.Dispose();
            image = null;

            if (m_BgTexture != null)
            {
                this.m_BgTexture.Dispose();
                this.m_BgTexture = null;
            }

            this.m_BgTexture = bitmap;

            CreateVertexList();

            this.Invalidate();

            return true;
        }

        private void CreateVertexList()
        {
            {
                int size = (this.m_BgTexture.Height+1) * (this.m_BgTexture.Width+1);
                this.m_VertexList = new VertexData[size];
            }

            {
                this.m_PolygonList = new List<PolygonData>();
                this.m_PolygonList.Clear();
            }

            int pitch = this.m_BgTexture.Width + 1;

            // Vertex
            for (int y = 0, ynum=this.m_BgTexture.Height + 1; y<ynum; ++y)
            {
                for (int x = 0, xnum = this.m_BgTexture.Width + 1; x < xnum; ++x)
                {
                    SetVertex(x, y, pitch);
                }
            }

            // Polygon
            for (int y = 0, ynum = this.m_BgTexture.Height; y < ynum; ++y)
            {
                for (int x = 0, xnum = this.m_BgTexture.Width; x < xnum; ++x)
                {
                    //TODO: ここで四角形ポリゴン同士が同じ色だった場合はポリゴンを合体したい
                    // |/|/| -> | / |
                    AddPolygon(x, y, pitch);
                }
            }

            this.Text = "PolygonCount:" + (this.m_PolygonList.Count * 2);

            m_bUpdate = true;
        }

        private void SetVertex(int x, int y, int pitch)
        {
            float tx = Math.Min(Math.Max((float)x - 0.5f, 0.0f), (float)this.m_BgTexture.Width - 1);
            float ty = Math.Min(Math.Max((float)y - 0.5f, 0.0f), (float)this.m_BgTexture.Height - 1);

            PointF p0 = new PointF();
            p0.X = tx / (float)(this.m_BgTexture.Width - 1);
            p0.Y = ty / (float)(this.m_BgTexture.Height - 1);

            Color c0 = this.m_BgTexture.GetPixel((int)tx, (int)ty);

            VertexData vertex = new VertexData();
            vertex.m_Position = p0;
            vertex.m_Color = c0;
            this.m_VertexList[(y * pitch) + x] = vertex;
        }

        private void AddPolygon(int x, int y, int pitch)
        {
            int v0, v1, v2, v3;
            if (((x & 1) ^ (y & 1)) == 0)
            {
                v0 = ((y + 0) * pitch) + (x + 0);
                v1 = ((y + 0) * pitch) + (x + 1);
                v2 = ((y + 1) * pitch) + (x + 0);
                v3 = ((y + 1) * pitch) + (x + 1);
            }
            else
            {
                v0 = ((y + 0) * pitch) + (x + 1); // c1
                v1 = ((y + 0) * pitch) + (x + 0); // c0
                v2 = ((y + 1) * pitch) + (x + 1); // c3
                v3 = ((y + 1) * pitch) + (x + 0); // c2
            }

            Color c0, c1, c2, c3;
            c0 = this.m_VertexList[v0].m_Color;
            c1 = this.m_VertexList[v1].m_Color;
            c2 = this.m_VertexList[v2].m_Color;
            c3 = this.m_VertexList[v3].m_Color;

            // 同じ色かつ抜き職
            if ((c0.ToArgb() == Color.White.ToArgb()) && (c0.ToArgb() == c1.ToArgb()) && (c1.ToArgb() == c2.ToArgb()) && (c2.ToArgb() == c3.ToArgb()))
            {
                return;
            }

            if (!((c0.ToArgb() == Color.White.ToArgb()) && (c0.ToArgb() == c1.ToArgb()) && (c1.ToArgb() == c2.ToArgb())))
            {
                PolygonData polygon = new PolygonData();
                polygon.m_Index0 = v0;
                polygon.m_Index1 = v1;
                polygon.m_Index2 = v2;
                this.m_PolygonList.Add(polygon);
            }

            if (!((c3.ToArgb() == Color.White.ToArgb()) && (c3.ToArgb() == c2.ToArgb()) && (c2.ToArgb() == c1.ToArgb())))
            {
                PolygonData polygon = new PolygonData();
                polygon.m_Index0 = v3;
                polygon.m_Index1 = v2;
                polygon.m_Index2 = v1;
                this.m_PolygonList.Add(polygon);
            }
        }

        struct VertexData
        {
            public PointF m_Position;
            public Color m_Color;
        };

        struct PolygonData
        {
            public int m_Index0;
            public int m_Index1;
            public int m_Index2;
        };

        private Bitmap m_BgTexture = null;
        private VertexData[] m_VertexList = null;
        private List<PolygonData> m_PolygonList = null;
        private bool m_bUpdate = false;
    }
}
