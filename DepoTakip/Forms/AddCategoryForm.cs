using System;
using System.Drawing;
using System.Windows.Forms;
using DepoTakip.DataAccess;
using DepoTakip.Models;

namespace DepoTakip.Forms
{
    public class AddCategoryForm : Form
    {
        private readonly DatabaseContext _context;
        private TextBox txtCategoryName;
        private Button btnSave;

        public AddCategoryForm(DatabaseContext context)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string iconPath = Path.Combine(basePath, "Resources", "uguricon.ico");
            this.Icon = new Icon(iconPath);
            _context = context;

            // === MODERN TASARIM ===
            this.Text = "Yeni Kategori Ekle";
            this.Size = new Size(370, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.FromArgb(248, 252, 255);

            // Büyük başlık
            Label lblTitle = new Label
            {
                Text = "Kategori Ekle",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(39, 87, 130),
                Location = new Point(24, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // Kategori Adı Label
            Label lblName = new Label
            {
                Text = "Kategori Adı",
                Location = new Point(24, 65),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(85, 90, 110),
                AutoSize = true
            };
            this.Controls.Add(lblName);

            // TextBox (modern padding ve odak efekti)
            txtCategoryName = new TextBox
            {
                Location = new Point(24, 88),
                Width = 300,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            txtCategoryName.GotFocus += (s, e) => txtCategoryName.BackColor = Color.FromArgb(235, 245, 255);
            txtCategoryName.LostFocus += (s, e) => txtCategoryName.BackColor = Color.White;
            this.Controls.Add(txtCategoryName);

            // Kaydet butonu (modern görünüm)
            btnSave = new Button
            {
                Text = "Kaydet",
                Size = new Size(110, 34),
                Location = new Point(214, 125),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(123, 210, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.MouseEnter += (s, e) => btnSave.BackColor = Color.FromArgb(68, 180, 255);
            btnSave.MouseLeave += (s, e) => btnSave.BackColor = Color.FromArgb(123, 210, 255);

            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCategoryName.Text))
            {
                MessageBox.Show("Kategori adı boş olamaz!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var newCategory = new Category { Name = txtCategoryName.Text.Trim() };
            _context.Categories.Add(newCategory);
            _context.SaveChanges();

            MessageBox.Show("Kategori başarıyla eklendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}
