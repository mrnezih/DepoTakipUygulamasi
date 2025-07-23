// Forms/ProductEntryForm.cs

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DepoTakip.DataAccess;
using DepoTakip.Models;
using ExcelDataReader;
using System.Data;

namespace DepoTakip.Forms
{
    public class ProductEntryForm : Form
    {
        private readonly DatabaseContext _context;
        private TextBox txtProductName, txtBrand;
        private ComboBox cmbCategory;
        private NumericUpDown numQuantity;
        private Button btnSave;

        // Düzenlenecek kaydın ID'sini tutmak için. Null ise yeni kayıt, dolu ise düzenleme modu.
        private int? _entryId;

        // Yeni kayıt için mevcut constructor
        public ProductEntryForm(DatabaseContext context) : this(context, null)
        {
        }

        // Düzenleme için yeni constructor
        public ProductEntryForm(DatabaseContext context, int? entryId)
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
this.MaximizeBox = false;
this.MinimizeBox = false; 

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
string iconPath = Path.Combine(basePath, "Resources", "uguricon.ico");
this.Icon = new Icon(iconPath);
            _context = context;
            _entryId = entryId; 

            this.Text = _entryId.HasValue ? "Ürün Girişini Düzenle" : "Yeni Ürün Girişi"; 
            this.Size = new Size(350, 250);
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeControls();
            LoadCategories();

           
            if (_entryId.HasValue)
            {
                LoadEntryData();
            }
        }

       private void InitializeControls()
{
    this.BackColor = Color.FromArgb(244, 250, 255);

    // Ürün Adı
    var lblProductName = new Label
    {
        Text = "Ürün Adı:",
        Location = new Point(20, 26),
        AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(39, 87, 130)
    };
    txtProductName = new TextBox
    {
        Location = new Point(120, 22),
        Width = 200,
        Font = new Font("Segoe UI", 10)
    };

    // Marka
    var lblBrand = new Label
    {
        Text = "Marka:",
        Location = new Point(20, 61),
        AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(39, 87, 130)
    };
    txtBrand = new TextBox
    {
        Location = new Point(120, 57),
        Width = 200,
        Font = new Font("Segoe UI", 10)
    };

    // Kategori
    var lblCategory = new Label
    {
        Text = "Kategori:",
        Location = new Point(20, 96),
        AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(39, 87, 130)
    };
    cmbCategory = new ComboBox
    {
        Location = new Point(120, 92),
        Width = 200,
        Font = new Font("Segoe UI", 10),
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    // Miktar
    var lblQuantity = new Label
    {
        Text = "Miktar:",
        Location = new Point(20, 131),
        AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(39, 87, 130)
    };
    numQuantity = new NumericUpDown
    {
        Location = new Point(120, 127),
        Width = 200,
        Font = new Font("Segoe UI", 10),
        Minimum = 1,
        Maximum = 10000
    };

    // Excelden Yükle
    Button btnImportExcel = new Button
    {
        Text = "Excelden Yükle",
        Location = new Point(120, 170),
        Width = 120,
        Height = 32,
        BackColor = Color.LightGreen,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
    };
    btnImportExcel.Click += BtnImportExcel_Click;
    btnImportExcel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
    btnImportExcel.Location = new Point(20, this.ClientSize.Height - btnImportExcel.Height - 10);


    // Kaydet
            btnSave = new Button
    {
        Text = "Kaydet",
        Location = new Point(250, 170),
        Width = 70,
        Height = 32,
        BackColor = Color.FromArgb(123, 210, 255),
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(39, 87, 130)
    };
    btnSave.Click += BtnSave_Click;

    this.Controls.Add(lblProductName);
    this.Controls.Add(txtProductName);
    this.Controls.Add(lblBrand);
    this.Controls.Add(txtBrand);
    this.Controls.Add(lblCategory);
    this.Controls.Add(cmbCategory);
    this.Controls.Add(lblQuantity);
    this.Controls.Add(numQuantity);
    this.Controls.Add(btnImportExcel);
    this.Controls.Add(btnSave);
}


        private void LoadCategories()
        {
            cmbCategory.DataSource = _context.Categories.ToList();
            cmbCategory.DisplayMember = "Name";
            cmbCategory.ValueMember = "Name";
        }

      
        private void LoadEntryData()
        {
            var entry = _context.ProductEntries.Find(_entryId.Value);
            if (entry != null)
            {
                txtProductName.Text = entry.ProductName;
                txtBrand.Text = entry.Brand;
                cmbCategory.SelectedValue = entry.CategoryName;
                numQuantity.Value = entry.Quantity;
            }
        }

private void BtnImportExcel_Click(object sender, EventArgs e)
{
    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    using (OpenFileDialog ofd = new OpenFileDialog())
    {
        ofd.Filter = "Excel Dosyaları|*.xls;*.xlsx";
        ofd.Title = "Excel Dosyası Seçin";

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                using (var stream = File.Open(ofd.FileName, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                    {
                        var ds = reader.AsDataSet();
                        if (ds.Tables.Count == 0)
                        {
                            MessageBox.Show("Excel dosyasında veri yok!", "Hata");
                            return;
                        }
                        var table = ds.Tables[0];
                        if (table.Rows.Count < 2)
                        {
                            MessageBox.Show("Excel dosyasında başlık veya veri yok!", "Hata");
                            return;
                        }

                       
                        string Normalize(string input)
                        {
                            return input.Trim().ToLower()
                                .Replace("ı", "i").Replace("ü", "u")
                                .Replace("ö", "o").Replace("ç", "c")
                                .Replace("ş", "s").Replace("ğ", "g")
                                .Replace(" ", "");
                        }

                        var headerRow = table.Rows[0].ItemArray.Select(x => Normalize(x.ToString())).ToList();
                        int idxProductName = headerRow.FindIndex(h => h.Contains("urunadi"));
                        int idxBrand       = headerRow.FindIndex(h => h.Contains("marka"));
                        int idxCategory    = headerRow.FindIndex(h => h.Contains("kategori"));
                        int idxQuantity    = headerRow.FindIndex(h => h.Contains("miktar"));

                        if (idxProductName == -1 || idxBrand == -1 || idxCategory == -1 || idxQuantity == -1)
                        {
                            MessageBox.Show("Excel başlıkları hatalı! 'Ürün Adı', 'Marka', 'Kategori', 'Miktar' başlıkları olmalı.", "Hata");
                            return;
                        }

                        int addedCount = 0;
                        using (var db = new DepoTakip.DataAccess.DatabaseContext())
                        {
                            for (int i = 1; i < table.Rows.Count; i++)
                            {
                                var row = table.Rows[i];
                                if (row.ItemArray.All(x => string.IsNullOrWhiteSpace(x?.ToString()))) continue;

                                string productName = row[idxProductName]?.ToString().Trim();
                                string brand       = row[idxBrand]?.ToString().Trim();
                                string category    = row[idxCategory]?.ToString().Trim();
                                int quantity = 0;
                                int.TryParse(row[idxQuantity]?.ToString(), out quantity);

                                if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(category) || quantity <= 0)
                                    continue;

                                var cat = db.Categories.FirstOrDefault(c => c.Name == category);
                                if (cat == null)
                                {
                                    cat = new DepoTakip.Models.Category { Name = category };
                                    db.Categories.Add(cat);
                                    db.SaveChanges();
                                }

                                var exists = db.ProductEntries.Any(p =>
                                    p.ProductName == productName &&
                                    p.Brand == brand &&
                                    p.CategoryName == category);
                                if (!exists)
                                {
                                    var entry = new DepoTakip.Models.ProductEntry
                                    {
                                        ProductName = productName,
                                        Brand = brand,
                                        CategoryName = category,
                                        Quantity = quantity,
                                        EntryDate = DateTime.Now
                                    };
                                    db.ProductEntries.Add(entry);
                                    addedCount++;
                                }
                            }
                            db.SaveChanges();
                        }

                        MessageBox.Show($"{addedCount} ürün başarıyla eklendi.", "Başarılı");
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Excelden ürün yüklerken hata oluştu: " + ex.Message);
            }
        }
    }
}
        private void ImportExcel(string filePath)
        {
          
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var dataSet = reader.AsDataSet();
                    var dataTable = dataSet.Tables[0];

                   
                    var headerRow = dataTable.Rows[0].ItemArray.Select(x => x.ToString().Trim().ToLower()).ToList();
                    var columnIndex = new Dictionary<string, int>();
                    for (int i = 0; i < headerRow.Count; i++)
                        columnIndex[headerRow[i]] = i;

                    // Gerekli başlıklar kontrolü (opsiyonel)
                    var required = new[] { "ürün adı", "marka", "kategori", "miktar" };
                    foreach (var col in required)
                        if (!columnIndex.ContainsKey(col))
                        {
                            MessageBox.Show($"Excel’de '{col}' başlığı bulunamadı.");
                            return;
                        }

                   
                    for (int i = 1; i < dataTable.Rows.Count; i++)
                    {
                        var row = dataTable.Rows[i];
                        string productName = row[columnIndex["ürün adı"]]?.ToString();
                        string brand = row[columnIndex["marka"]]?.ToString();
                        string category = row[columnIndex["kategori"]]?.ToString();
                        int quantity = int.Parse(row[columnIndex["miktar"]]?.ToString());

                      
                    }
                    MessageBox.Show("Excel’den ürünler başarıyla aktarıldı!");
                }
            }
        }

     
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProductName.Text) || cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_entryId.HasValue) // Düzenleme Modu
            {
                var entryToUpdate = _context.ProductEntries.Find(_entryId.Value);
                if (entryToUpdate != null)
                {
                    entryToUpdate.ProductName = txtProductName.Text.Trim();
                    entryToUpdate.Brand = txtBrand.Text.Trim();
                    entryToUpdate.CategoryName = cmbCategory.SelectedValue.ToString();
                    entryToUpdate.Quantity = (int)numQuantity.Value;

                    _context.ProductEntries.Update(entryToUpdate);
                    MessageBox.Show("Giriş başarıyla güncellendi.", "Başarılı");
                }
            }
            else // Yeni Kayıt Modu
            {
                var entry = new ProductEntry
                {
                    ProductName = txtProductName.Text.Trim(),
                    Brand = txtBrand.Text.Trim(),
                    CategoryName = cmbCategory.SelectedValue.ToString(),
                    Quantity = (int)numQuantity.Value,
                    EntryDate = DateTime.Now
                };
                _context.ProductEntries.Add(entry);
                MessageBox.Show("Ürün girişi başarıyla yapıldı.", "Başarılı");
            }

            _context.SaveChanges();
            this.Close();
        }
   
   
   
   
   
    }
    
}