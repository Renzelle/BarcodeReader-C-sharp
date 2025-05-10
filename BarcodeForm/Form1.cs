using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using System.Drawing.Printing;
using System.Drawing.Imaging;
using System.IO;
using OfficeOpenXml;  // EPPlus for Excel
using OfficeOpenXml.Drawing;

namespace BarcodeForm
{
    public partial class Form1 : Form
    {
        private string connectionString = "server=localhost;database=barcode;user=root;password='';";

        public Form1()
        {
            InitializeComponent();
        }

        private void LoadData()
        {
            try
            {
                string query = "SELECT * FROM masterfile";
                DataTable dt = new DataTable();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }

                dataGridView1.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

                string barcodeData = selectedRow.Cells["Barcode"].Value.ToString().PadLeft(12, '0'); // Ensure 12 digits for EAN-13
                string topText = "" + selectedRow.Cells["BranchDesc"].Value.ToString();
                string unitPrice = selectedRow.Cells["UnitPrice"].Value.ToString();
                string formattedPrice = unitPrice.Replace(",", " , ").Replace(".", " . ");
                string bottomText = "P" + formattedPrice;

                string leftBottomText = "" + selectedRow.Cells["Model"].Value.ToString();

                Bitmap barcodeImage = GenerateBarcode(barcodeData, topText, bottomText, leftBottomText);
                if (barcodeImage != null)
                {
                    pictureBox1.Image = barcodeImage;
                }
            }
            else
            {
                MessageBox.Show("Please select a row from the data grid.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (FontFamily font in System.Drawing.FontFamily.Families)
            {
                comboBoxFonts.Items.Add(font.Name);
            }
            comboBoxFonts.SelectedIndex = 0; // Set default font
            LoadData();
        }

        private Bitmap GenerateBarcode(string data, string topText, string bottomText, string leftBottomText)
        {
            if (data.Length != 12)
            {
                MessageBox.Show("EAN-13 requires 12 digits.", "Invalid Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            string selectedFontName = comboBoxFonts.SelectedItem?.ToString() ?? "Arial"; // Default to Arial

            int barcodeWidth = 400;
            int barcodeHeight = 50;

            BarcodeWriter barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.EAN_13,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = barcodeWidth,
                    Height = barcodeHeight,
                    Margin = 10,
                    PureBarcode = true
                },
                Renderer = new ZXing.Rendering.BitmapRenderer
                {
                    Foreground = Color.Black,
                    Background = Color.White
                }
            };

            Bitmap barcodeBitmap = barcodeWriter.Write(data);

            Bitmap finalBitmap = new Bitmap(barcodeWidth + 20, barcodeHeight + 100, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(finalBitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                g.DrawImage(barcodeBitmap, 2, 30, barcodeWidth, barcodeHeight);

                using (Font font = new Font(selectedFontName, 18, FontStyle.Bold))
                using (Font modelFont = new Font(selectedFontName, 16, FontStyle.Regular))
                using (Font barcodeFont = new Font(selectedFontName, 18, FontStyle.Regular))
                using (SolidBrush brush = new SolidBrush(Color.Black))
                {
                    //Top Text
                    SizeF topTextSize = g.MeasureString(topText, font);
                    g.DrawString(topText, font, brush, (finalBitmap.Width - topTextSize.Width) / 2, 1);
                    //Center Bottom Text
                    SizeF bottomTextSize = g.MeasureString(bottomText, font);
                    g.DrawString(bottomText, font, brush, (finalBitmap.Width - bottomTextSize.Width) / 2, finalBitmap.Height - 35);
                    //Left Bottom Text
                    g.DrawString(leftBottomText, modelFont, brush, 35, finalBitmap.Height - 50);
                    //Center Barcode Text
                    string formattedBarcode = $"{data[0]} {data.Substring(1, 6)} {data.Substring(7, 5)} {data[11]}";
                    SizeF barcodeTextSize = g.MeasureString(formattedBarcode, barcodeFont);
                    g.DrawString(formattedBarcode, barcodeFont, brush, (finalBitmap.Width - barcodeTextSize.Width) / 2, barcodeHeight + 27);
                }
            }

            return finalBitmap;
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                int dpi = 30; // High DPI
                int printWidth = (int)(pictureBox1.Image.Width * (dpi / 96.0));
                int printHeight = (int)(pictureBox1.Image.Height * (dpi / 46.0));

                e.Graphics.DrawImage(pictureBox1.Image, new Rectangle(0, 0, printWidth, printHeight));
            }
        }

        private void buttonPrint_Click_1(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                PrintDocument printDocument = new PrintDocument();
                printDocument.PrintPage += new PrintPageEventHandler(PrintDocument_PrintPage);

                PrintDialog printDialog = new PrintDialog();
                printDialog.Document = printDocument;

                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    printDocument.Print();
                }
            }
            else
            {
                MessageBox.Show("No barcode to print!", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void buttonDownload_Click_1(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string folderPath = "Barcodes"; // Folder where barcodes will be saved
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            foreach (DataGridViewRow gridRow in dataGridView1.Rows)
            {
                if (!gridRow.IsNewRow) // Ignore empty new rows
                {
                    string barcodeData = gridRow.Cells["Barcode"].Value.ToString().PadLeft(12, '0');
                    string branch = gridRow.Cells["BranchDesc"].Value.ToString();
                    string model = gridRow.Cells["Model"].Value.ToString();
                    string price = "P" + gridRow.Cells["UnitPrice"].Value.ToString();

                    // Generate barcode image
                    Bitmap barcodeImage = GenerateBarcode(barcodeData, branch, price, model);
                    if (barcodeImage != null)
                    {
                        string imagePath = Path.Combine(folderPath, $"{barcodeData}.png");
                        barcodeImage.Save(imagePath, ImageFormat.Png);
                    }
                }
            }

            MessageBox.Show("All barcode images have been saved!", "Download Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Open the folder automatically
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
        }
    }
}