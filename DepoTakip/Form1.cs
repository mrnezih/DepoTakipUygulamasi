using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DepoTakip.DataAccess;
using DepoTakip.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data.SQLite;
using ExcelDataReader;
using System.Timers;

namespace DepoTakip
{
    public partial class Form1 : Form
    {
        private TabControl mainTabControl;
        private DataGridView dgvStockList, dgvProductEntries, dgvProductUsages;
        private Button btnAddCategory, btnAddNewProduct, btnRemoveProduct;
        private ComboBox cmbCategoryFilter, cmbLevelFilter;
        private Button btnClearFilter, btnExportToExcel;
        private TextBox txtStockSearch, txtEntrySearch, txtUsageSearch;
        private List<ProductEntry> allProductEntries = new List<ProductEntry>();
        private List<ProductUsage> allProductUsages = new List<ProductUsage>();
        private readonly DatabaseContext _context = new DatabaseContext();
        private Dictionary<string, bool> productEntrySortOrders = new Dictionary<string, bool>();
        private Dictionary<string, bool> productUsageSortOrders = new Dictionary<string, bool>();
        private Dictionary<string, bool> stockListSortOrders = new Dictionary<string, bool>();
        private List<dynamic> currentStockList = new List<dynamic>();
        private Panel filterPanel;
        private Label lblStockSearch, lblEntrySearch, lblUsageSearch;

        public Form1()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
string iconPath = Path.Combine(basePath, "Resources", "uguricon.ico");
this.Icon = new Icon(iconPath);
            InitializeComponent();
            SetupMonthlyBackup(); 
            SetupUI();
            this.Load += Form1_Load;
        }

        //YEDEKLEME
        private void SetupMonthlyBackup()
{
   System.Timers.Timer monthlyTimer = new System.Timers.Timer();
    monthlyTimer.Interval = TimeSpan.FromHours(1).TotalMilliseconds; // Her saat kontrol
    monthlyTimer.Elapsed += MonthlyBackupCheck;
    monthlyTimer.AutoReset = true;
    monthlyTimer.Enabled = true;
}

private void MonthlyBackupCheck(object sender, ElapsedEventArgs e)
{
    DateTime now = DateTime.Now;

    // Eğer saat 00:00-00:59 arasındaysa ve daha önce o gün yedek alınmamışsa
    string backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
    Directory.CreateDirectory(backupFolder);

    string todayBackupFile = Path.Combine(backupFolder, $"DepoTakip_{now:yyyy_MM_dd}.db");

    if (now.Day == 1 && now.Hour == 0 && !File.Exists(todayBackupFile))
    {
        string source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DepoTakip.db");
        File.Copy(source, todayBackupFile);
    }
}

        private void BtnBackup_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Yedek klasörü var mı bak, yoksa oluştur:
                string backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!Directory.Exists(backupFolder))
                    Directory.CreateDirectory(backupFolder);

                // 2. Dosya adını belirle
                string backupFileName = $"DepoTakip_Yedek_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                string destPath = Path.Combine(backupFolder, backupFileName);

                // 3. Context ve bağlantıları temizle
                dgvProductEntries.DataSource = null;
                dgvProductUsages.DataSource = null;
                dgvStockList.DataSource = null;
                _context?.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // 4. Yedekle
                string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DepoTakip.db");
                using (var source = new SQLiteConnection($"Data Source={sourcePath};Version=3;"))
                using (var dest = new SQLiteConnection($"Data Source={destPath};Version=3;"))
                {
                    source.Open();
                    dest.Open();
                    source.BackupDatabase(dest, "main", "main", -1, null, 0);
                }

                MessageBox.Show($"Yedek başarıyla kaydedildi!\n{destPath}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Restart();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Yedekleme sırasında hata oluştu: " + ex.Message);
            }
        }

        //YEDEKTEN ÇEKME
        private void BtnRestore_Click(object sender, EventArgs e)
        {
            // 1. Backups klasörü yolu:
            string backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(backupFolder))
            {
                MessageBox.Show("Henüz hiç yedek alınmamış!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.InitialDirectory = backupFolder;
                ofd.Filter = "SQLite Veritabanı (*.db)|*.db";
                ofd.Title = "Yedeği Yükle";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _context?.Dispose();
                        System.Data.SQLite.SQLiteConnection.ClearAllPools();

                        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DepoTakip.db");
                        File.Copy(ofd.FileName, dbPath, true);

                        MessageBox.Show("Yedek başarıyla geri yüklendi! Program yeniden başlatılıyor.");
                        System.Diagnostics.Process.Start(Application.ExecutablePath);
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Geri yükleme sırasında hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadFilterOptions();
            LoadData();
            UpdateSearchBoxVisibility();
        }

        public void LoadData(string categoryFilter = "Tümü", string levelFilter = "Tümü")
        {
            dgvProductEntries.DataSource = null;
            dgvProductUsages.DataSource = null;

            var entriesQuery = _context.ProductEntries.AsQueryable();
            if (categoryFilter != "Tümü")
                entriesQuery = entriesQuery.Where(p => p.CategoryName == categoryFilter);

            // --- Orijinal giriş listesini güncelle ---
            allProductEntries = entriesQuery.OrderByDescending(p => p.EntryDate).ToList();
            dgvProductEntries.DataSource = allProductEntries;
            AddActionButtonsToGrid(dgvProductEntries);
            dgvProductEntries.Columns["Id"].Visible = false;
            dgvProductEntries.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProductEntries.Columns["EditButton"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvProductEntries.Columns["EditButton"].Width = 35;
            dgvProductEntries.Columns["DeleteButton"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvProductEntries.Columns["DeleteButton"].Width = 35;

            var usagesQuery = _context.ProductUsages.AsQueryable();
            if (categoryFilter != "Tümü")
                usagesQuery = usagesQuery.Where(u => u.CategoryName == categoryFilter);
            if (levelFilter != "Tümü")
                usagesQuery = usagesQuery.Where(u => u.UserLevel == levelFilter);

            // --- Orijinal kullanım listesini güncelle ---
            allProductUsages = usagesQuery.OrderByDescending(u => u.UsageDate).ToList();
            dgvProductUsages.DataSource = allProductUsages;
            AddActionButtonsToGrid(dgvProductUsages);
            dgvProductUsages.Columns["Id"].Visible = false;
            dgvProductUsages.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProductUsages.Columns["EditButton"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvProductUsages.Columns["EditButton"].Width = 35;
            dgvProductUsages.Columns["DeleteButton"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvProductUsages.Columns["DeleteButton"].Width = 35;

            var allEntries = _context.ProductEntries.ToList();
            var allUsages = _context.ProductUsages.ToList();
            var entries = allEntries.GroupBy(p => new { p.ProductName, p.Brand, p.CategoryName })
                .Select(g => new { g.Key.ProductName, g.Key.Brand, g.Key.CategoryName, TotalEntry = g.Sum(p => p.Quantity) });
            var usages = allUsages.GroupBy(u => u.ProductName)
                .Select(g => new { ProductName = g.Key, TotalUsage = g.Sum(u => u.Quantity) }).ToList();
            var stockListQuery = from entry in entries
                                 join usage in usages on entry.ProductName equals usage.ProductName into usageGroup
                                 from u in usageGroup.DefaultIfEmpty()
                                 select new
                                 {
                                     ÜrünAdı = entry.ProductName,
                                     Marka = entry.Brand,
                                     Kategori = entry.CategoryName,
                                     KalanMiktar = entry.TotalEntry - (u?.TotalUsage ?? 0)
                                 };
            if (categoryFilter != "Tümü")
                stockListQuery = stockListQuery.Where(s => s.Kategori == categoryFilter);

            currentStockList = stockListQuery.ToList<dynamic>();
            dgvStockList.DataSource = currentStockList;

            dgvProductEntries.Columns["ProductName"].HeaderText = "Ürün Adı";
            dgvProductEntries.Columns["Brand"].HeaderText = "Marka";
            dgvProductEntries.Columns["CategoryName"].HeaderText = "Kategori";
            dgvProductEntries.Columns["Quantity"].HeaderText = "Miktar";
            dgvProductEntries.Columns["EntryDate"].HeaderText = "Giriş Tarihi";

            dgvProductUsages.Columns["ProductName"].HeaderText = "Ürün Adı";
            dgvProductUsages.Columns["CategoryName"].HeaderText = "Kategori";
            dgvProductUsages.Columns["Brand"].HeaderText = "Marka";
            dgvProductUsages.Columns["UsedBy"].HeaderText = "Kullanan Kişi";
            dgvProductUsages.Columns["UserLevel"].HeaderText = "Kademe";
            dgvProductUsages.Columns["Quantity"].HeaderText = "Miktar";
            dgvProductUsages.Columns["UsageDate"].HeaderText = "Kullanım Tarihi";

          
            if (txtStockSearch != null) ApplyStockSearch();
            if (txtEntrySearch != null) ApplyEntrySearch();
            if (txtUsageSearch != null) ApplyUsageSearch();
        }
        private void LoadFilterOptions()
        {
            var categories = _context.Categories.Select(c => c.Name).ToList();
            categories.Insert(0, "Tümü");
            cmbCategoryFilter.DataSource = categories;

            var levels = _context.ProductUsages.Select(u => u.UserLevel).Distinct().ToList();
            levels.Insert(0, "Tümü");
            cmbLevelFilter.DataSource = levels;
        }

        private void DgvStockList_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string columnName = dgvStockList.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrWhiteSpace(columnName)) return;
            bool ascending = true;
            if (stockListSortOrders.ContainsKey(columnName))
                ascending = !stockListSortOrders[columnName];
            stockListSortOrders[columnName] = ascending;
            var sortedList = ascending
                ? currentStockList.OrderBy(x => x.GetType().GetProperty(columnName).GetValue(x)).ToList()
                : currentStockList.OrderByDescending(x => x.GetType().GetProperty(columnName).GetValue(x)).ToList();
            dgvStockList.DataSource = sortedList;
        }

        // ===== TASARIM ve ARAMA KUTULARI filterPanel'in ALTINDA =====
        private void SetupUI()
        {
            // Genel Form Ayarı
            this.Text = "BATIKENT UĞUR OKULLARI DEPO TAKİP";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorTranslator.FromHtml("#F4F6FA");

            // -- ÜST PANEL MODERN TASARIM --
            filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.FromArgb(244, 249, 255), // Açık mavi/gri
                Padding = new Padding(16, 8, 16, 8),
                BorderStyle = BorderStyle.FixedSingle
            };


            Label lblKategori = new Label
            {
                Text = "Kategori:",
                Location = new Point(15, 22),
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 51, 86)
            };
            filterPanel.Controls.Add(lblKategori);

            cmbCategoryFilter = new ComboBox
            {
                Location = new Point(110, 18),
                Width = 180,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => { ApplyFilters(); UpdateSearchBoxVisibility(); };
            filterPanel.Controls.Add(cmbCategoryFilter);

            // Kademe Label
            Label lblKademe = new Label
            {
                Text = "Kademe:",
                Location = new Point(320, 22),
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 51, 86)
            };
            filterPanel.Controls.Add(lblKademe);

            // Kademe ComboBox
            cmbLevelFilter = new ComboBox
            {
                Location = new Point(410, 18),
                Width = 180,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cmbLevelFilter.SelectedIndexChanged += (s, e) => { ApplyFilters(); UpdateSearchBoxVisibility(); };
            filterPanel.Controls.Add(cmbLevelFilter);


            // LOGO
       
            PictureBox logoBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(140, 76),
                Location = new Point(filterPanel.Width - 140, 10), // Sağdan 20px boşluk bırak
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            try
            {
                string logoPath = Path.Combine(Application.StartupPath, "Resources", "yazilogo.png");
                if (File.Exists(logoPath))
                    logoBox.Image = Image.FromFile(logoPath);
                else
                    logoBox.BackColor = Color.Red;
            }
            catch { logoBox.BackColor = Color.Red; }
            filterPanel.Controls.Add(logoBox);

     
            filterPanel.Resize += (s, e) =>
            {
            
                logoBox.Left = filterPanel.Width - logoBox.Width - 15;
                logoBox.Top = 8;
            };


            // Filtreyi Temizle Butonu (Modern)
            // btnClearFilter = new Button
            //{
            //    Text = "Filtreyi Temizle",
            //     Location = new Point(610, 22),
            //    Height = 36,
            //     Width = 160,
            //     BackColor = ColorTranslator.FromHtml("#fff3cd"),
            //     ForeColor = ColorTranslator.FromHtml("#735c0f"),
            //     FlatStyle = FlatStyle.Flat,
            //     Font = new Font("Segoe UI", 11, FontStyle.Bold),
            //    Cursor = Cursors.Hand
            // };
            // btnClearFilter.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#ffe082");
            // btnClearFilter.Click += BtnClearFilter_Click;
            //filterPanel.Controls.Add(btnClearFilter);

            // Arama kutusu
            lblStockSearch = new Label { Text = "Ara:", Location = new Point(20, 65), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            txtStockSearch = new TextBox { Location = new Point(110, 62), Width = 170, Font = new Font("Segoe UI", 11) };
            filterPanel.Controls.Add(lblStockSearch);
            filterPanel.Controls.Add(txtStockSearch);

            lblEntrySearch = new Label { Text = "Ara:", Location = new Point(20, 65), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Visible = false };
            txtEntrySearch = new TextBox { Location = new Point(110, 62), Width = 170, Font = new Font("Segoe UI", 11), Visible = false };
            filterPanel.Controls.Add(lblEntrySearch);
            filterPanel.Controls.Add(txtEntrySearch);

            lblUsageSearch = new Label { Text = "Ara:", Location = new Point(20, 65), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Visible = false };
            txtUsageSearch = new TextBox { Location = new Point(110, 62), Width = 170, Font = new Font("Segoe UI", 11), Visible = false };
            filterPanel.Controls.Add(lblUsageSearch);
            filterPanel.Controls.Add(txtUsageSearch);

            this.Controls.Add(filterPanel);

            // --- Tab ve DataGridView Alanları ---
            mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Padding = new Point(16, 4) 
            };
            mainTabControl.SelectedIndexChanged += (s, e) => { ToggleLevelFilter(); UpdateSearchBoxVisibility(); };
            this.Controls.Add(mainTabControl);
            mainTabControl.BringToFront();

            ImageList tabImageList = new ImageList();
            tabImageList.ImageSize = new Size(35, 35);
            tabImageList.Images.Add("stock", Image.FromFile("Resources/stocklogo.png"));
            tabImageList.Images.Add("entry", Image.FromFile("Resources/entrylogo.png"));
            tabImageList.Images.Add("usage", Image.FromFile("Resources/usagelogo.png"));

            mainTabControl.ImageList = tabImageList;

            TabPage tabStock = new TabPage("Stok Listesi") { ImageIndex = 0 };
            TabPage tabEntries = new TabPage("Ürün Girişi") { ImageIndex = 1 };
            TabPage tabUsages = new TabPage("Ürün Kullanımı") { ImageIndex = 2 };

            mainTabControl.TabPages.Add(tabStock);
            mainTabControl.TabPages.Add(tabEntries);
            mainTabControl.TabPages.Add(tabUsages);


        
            dgvStockList = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.LightGray
            };
            dgvStockList.ColumnHeaderMouseClick += DgvStockList_ColumnHeaderMouseClick;
            dgvStockList.CellFormatting += DgvStockList_CellFormatting;
            tabStock.Controls.Add(dgvStockList);

            dgvProductEntries = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.LightGray
            };
            dgvProductEntries.ColumnHeaderMouseClick += DgvProductEntries_ColumnHeaderMouseClick;
            tabEntries.Controls.Add(dgvProductEntries);

            dgvProductUsages = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.LightGray
            };
            dgvProductUsages.ColumnHeaderMouseClick += DgvProductUsages_ColumnHeaderMouseClick;
            tabUsages.Controls.Add(dgvProductUsages);


            AddActionButtonsToGrid(dgvProductEntries);
            AddActionButtonsToGrid(dgvProductUsages);
            dgvProductEntries.CellContentClick += DgvProductEntries_CellContentClick;
            dgvProductUsages.CellContentClick += DgvProductUsages_CellContentClick;

            foreach (var dgv in new[] { dgvStockList, dgvProductEntries, dgvProductUsages })
            {
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(39, 87, 130);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv.EnableHeadersVisualStyles = false;
                dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10);
                dgv.DefaultCellStyle.BackColor = Color.White;
                dgv.DefaultCellStyle.ForeColor = Color.Black; // <-- satır yazısı her zaman siyah
                dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(57, 105, 138); // Seçili satır koyu mavi
                dgv.DefaultCellStyle.SelectionForeColor = Color.White; // Seçili satır yazısı beyaz
                dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
                dgv.GridColor = Color.LightSteelBlue;
                dgv.RowTemplate.Height = 30;
            }

            dgvStockList.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvStockList.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(220, 240, 255);
            };
            dgvStockList.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvStockList.Rows[e.RowIndex].DefaultCellStyle.BackColor =
                        e.RowIndex % 2 == 0 ? Color.White : Color.FromArgb(240, 248, 255);
            };

           
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(244, 250, 255)
            };
            this.Controls.Add(bottomPanel);

            Font buttonFont = new Font("Segoe UI", 10, FontStyle.Bold);

            // Sol: Excel'e Aktar
            btnExportToExcel = new Button
            {
                Text = "🗎 Excel'e Aktar",
                Size = new Size(130, 32),
                Location = new Point(18, 18),
                BackColor = Color.FromArgb(91, 155, 213),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = buttonFont,
                Cursor = Cursors.Hand
            };
            btnExportToExcel.FlatAppearance.BorderSize = 0;
            btnExportToExcel.Click += BtnExportToExcel_Click;
            bottomPanel.Controls.Add(btnExportToExcel);

            // Orta butonlar (daha küçük ve daha sola)
            Button btnBackup = new Button
            {
                Text = "⭳ Yedekle",
                Size = new Size(110, 32),
                BackColor = Color.FromArgb(255, 237, 158),
                ForeColor = Color.FromArgb(100, 75, 0),
                FlatStyle = FlatStyle.Flat,
                Font = buttonFont,
                Cursor = Cursors.Hand
            };
            btnBackup.FlatAppearance.BorderSize = 0;
            btnBackup.Click += BtnBackup_Click;
            bottomPanel.Controls.Add(btnBackup);

            Button btnRestore = new Button
            {
                Text = "⭱ Yedeği Yükle",
                Size = new Size(130, 32),
                BackColor = Color.FromArgb(255, 200, 200),
                ForeColor = Color.FromArgb(120, 35, 35),
                FlatStyle = FlatStyle.Flat,
                Font = buttonFont,
                Cursor = Cursors.Hand
            };
            btnRestore.FlatAppearance.BorderSize = 0;
            btnRestore.Click += BtnRestore_Click;
            bottomPanel.Controls.Add(btnRestore);

            // Sağ butonlar
            btnAddNewProduct = new Button
            {
                Text = "➕ Ürün Ekle",
                Size = new Size(125, 32),
                BackColor = Color.FromArgb(180, 255, 185),
                ForeColor = Color.FromArgb(30, 90, 30),
                FlatStyle = FlatStyle.Flat,
                Font = buttonFont,
                Cursor = Cursors.Hand
            };
            btnAddNewProduct.FlatAppearance.BorderSize = 0;
            btnAddNewProduct.Click += BtnAddNewProduct_Click;
            bottomPanel.Controls.Add(btnAddNewProduct);

            btnRemoveProduct = new Button
            {
                Text = "➖ Ürün Çıkar",
                Size = new Size(125, 32),
                BackColor = Color.FromArgb(255, 230, 175),
                ForeColor = Color.FromArgb(140, 75, 5),
                FlatStyle = FlatStyle.Flat,
                Font = buttonFont,
                Cursor = Cursors.Hand
            };
            btnRemoveProduct.FlatAppearance.BorderSize = 0;
            btnRemoveProduct.Click += BtnRemoveProduct_Click;
            bottomPanel.Controls.Add(btnRemoveProduct);

            btnAddCategory = new Button
            {
                Text = "📂 Kategori",
                Size = new Size(125, 32),
                BackColor = Color.FromArgb(195, 225, 255),
                ForeColor = Color.FromArgb(32, 58, 80),
                FlatStyle = FlatStyle.Flat,
                Font = buttonFont,
                Cursor = Cursors.Hand
            };
            btnAddCategory.FlatAppearance.BorderSize = 0;
            btnAddCategory.Click += BtnAddCategory_Click;
            bottomPanel.Controls.Add(btnAddCategory);

            // Footer yazısı (sol altta)
            LinkLabel footerLabel = new LinkLabel
            {
                Text = "nezihdogan.com / developed by Nezih Doğan",
                AutoSize = true,
                Location = new Point(18, bottomPanel.Height - 20),
                LinkColor = Color.DarkSlateGray,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };
            footerLabel.LinkClicked += (s, e) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.nezihdogan.com",
                    UseShellExecute = true
                });
            };
            bottomPanel.Controls.Add(footerLabel);


           bottomPanel.Controls.Add(btnExportToExcel);
bottomPanel.Controls.Add(btnBackup);
bottomPanel.Controls.Add(btnRestore);
bottomPanel.Controls.Add(btnAddNewProduct);
bottomPanel.Controls.Add(btnRemoveProduct);
bottomPanel.Controls.Add(btnAddCategory);
bottomPanel.Controls.Add(footerLabel);

// Responsive konumlandırma
void ArrangeBottomButtons()
{
    // Sol
    btnExportToExcel.Location = new Point(18, 18);

    // Orta 
    int panelWidth = bottomPanel.Width;
    int totalMiddle = btnBackup.Width + btnRestore.Width + 16;
    int leftOfMiddle = panelWidth / 2 - totalMiddle - 60; // ortanın SOLUNA kaydır
    btnBackup.Location = new Point(leftOfMiddle, 18);
    btnRestore.Location = new Point(leftOfMiddle + btnBackup.Width + 16, 18);

    // Sağdan sola dizeceğiz
    int space = 10;
    int rightX = panelWidth - (btnAddNewProduct.Width + btnRemoveProduct.Width + btnAddCategory.Width + 2 * space) - 18;
    btnAddNewProduct.Location = new Point(rightX, 18);
    btnRemoveProduct.Location = new Point(rightX + btnAddNewProduct.Width + space, 18);
    btnAddCategory.Location = new Point(rightX + btnAddNewProduct.Width + btnRemoveProduct.Width + 2 * space, 18);

    // Footer yazısı en solda, alta
    footerLabel.Location = new Point(18, bottomPanel.Height - footerLabel.Height - 2);
}

// Olay ve ilk çağrı
bottomPanel.Resize += (s, e) => ArrangeBottomButtons();
ArrangeBottomButtons();

            // Açılışta ilk konumlandırma
            bottomPanel.PerformLayout();




            // ARAMA EVENTLERİ
            txtStockSearch.TextChanged += (s, e) => ApplyStockSearch();
            txtEntrySearch.TextChanged += (s, e) => ApplyEntrySearch();
            txtUsageSearch.TextChanged += (s, e) => ApplyUsageSearch();
        }
        


      
        private void UpdateSearchBoxVisibility()
        {
            bool stockTab = mainTabControl.SelectedTab.Text == "Stok Listesi";
            bool entryTab = mainTabControl.SelectedTab.Text == "Ürün Girişi";
            bool usageTab = mainTabControl.SelectedTab.Text == "Ürün Kullanımı";

            lblStockSearch.Visible = txtStockSearch.Visible = stockTab;
            lblEntrySearch.Visible = txtEntrySearch.Visible = entryTab;
            lblUsageSearch.Visible = txtUsageSearch.Visible = usageTab;
        }

        // ==== ARAMA METOTLARI ====
        private void ApplyStockSearch()
        {
            string search = txtStockSearch.Text.ToLower();
            var filtered = currentStockList
                .Where(item =>
                    item.ÜrünAdı.ToLower().Contains(search) ||
                    item.Marka.ToLower().Contains(search) ||
                    item.Kategori.ToLower().Contains(search)
                ).ToList();
            dgvStockList.DataSource = filtered;
        }

        private void ApplyEntrySearch()
        {
            string search = txtEntrySearch.Text.ToLower();
            List<ProductEntry> list = allProductEntries;
            if (string.IsNullOrWhiteSpace(search))
            {
                dgvProductEntries.DataSource = allProductEntries;
            }
            else
            {
                var filtered = list.Where(item =>
                    item.ProductName.ToLower().Contains(search) ||
                    (item.Brand ?? "").ToLower().Contains(search) ||
                    (item.CategoryName ?? "").ToLower().Contains(search)
                ).ToList();
                dgvProductEntries.DataSource = filtered;
            }
            AddActionButtonsToGrid(dgvProductEntries);
            AdjustActionColumnSizes(dgvProductEntries);
        }

        private void ApplyUsageSearch()
        {
            string search = txtUsageSearch.Text.ToLower();
            List<ProductUsage> list = allProductUsages;
            if (string.IsNullOrWhiteSpace(search))
            {
                dgvProductUsages.DataSource = allProductUsages;
            }
            else
            {
                var filtered = list.Where(item =>
                    item.ProductName.ToLower().Contains(search) ||
                    (item.Brand ?? "").ToLower().Contains(search) ||
                    (item.CategoryName ?? "").ToLower().Contains(search) ||
                    (item.UsedBy ?? "").ToLower().Contains(search) ||
                    (item.UserLevel ?? "").ToLower().Contains(search)
                ).ToList();
                dgvProductUsages.DataSource = filtered;
            }
            AddActionButtonsToGrid(dgvProductUsages);
            AdjustActionColumnSizes(dgvProductUsages);
        }

       
        private void DgvProductEntries_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string columnName = dgvProductEntries.Columns[e.ColumnIndex].DataPropertyName;
            var currentList = (List<ProductEntry>)dgvProductEntries.DataSource;
            bool ascending = true;
            if (productEntrySortOrders.ContainsKey(columnName))
                ascending = !productEntrySortOrders[columnName];
            productEntrySortOrders[columnName] = ascending;
            var sortedList = ascending
                ? currentList.OrderBy(x => x.GetType().GetProperty(columnName).GetValue(x)).ToList()
                : currentList.OrderByDescending(x => x.GetType().GetProperty(columnName).GetValue(x)).ToList();
            dgvProductEntries.DataSource = sortedList;
            AddActionButtonsToGrid(dgvProductEntries);
            AdjustActionColumnSizes(dgvProductEntries);
        }
        private void DgvProductUsages_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string columnName = dgvProductUsages.Columns[e.ColumnIndex].DataPropertyName;
            var currentList = (List<ProductUsage>)dgvProductUsages.DataSource;
            bool ascending = true;
            if (productUsageSortOrders.ContainsKey(columnName))
                ascending = !productUsageSortOrders[columnName];
            productUsageSortOrders[columnName] = ascending;
            var sortedList = ascending
                ? currentList.OrderBy(x => x.GetType().GetProperty(columnName).GetValue(x)).ToList()
                : currentList.OrderByDescending(x => x.GetType().GetProperty(columnName).GetValue(x)).ToList();
            dgvProductUsages.DataSource = sortedList;
            AddActionButtonsToGrid(dgvProductUsages);
            AdjustActionColumnSizes(dgvProductUsages);
        }
        private void AdjustActionColumnSizes(DataGridView dgv)
        {
            if (dgv.Columns.Contains("EditButton"))
            {
                dgv.Columns["EditButton"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgv.Columns["EditButton"].Width = 35;
            }
            if (dgv.Columns.Contains("DeleteButton"))
            {
                dgv.Columns["DeleteButton"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgv.Columns["DeleteButton"].Width = 35;
            }
            if (dgv.Columns.Contains("Id"))
            {
                dgv.Columns["Id"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgv.Columns["Id"].Width = 50;
            }
        }
        private void AddActionButtonsToGrid(DataGridView dgv)
        {
            if (dgv.Columns.Contains("EditButton")) dgv.Columns.Remove("EditButton");
            if (dgv.Columns.Contains("DeleteButton")) dgv.Columns.Remove("DeleteButton");

            var editButton = new DataGridViewButtonColumn
            {
                Name = "EditButton",
                HeaderText = "",
                UseColumnTextForButtonValue = true,
                Text = "✏️",
                Width = 40,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI Emoji", 11),
                    Padding = new Padding(0)
                }
            };
            var deleteButton = new DataGridViewButtonColumn
            {
                Name = "DeleteButton",
                HeaderText = "",
                UseColumnTextForButtonValue = true,
                Text = "❌",
                Width = 40,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI Emoji", 11),
                    Padding = new Padding(0)
                }
            };
            dgv.Columns.Add(editButton);
            dgv.Columns.Add(deleteButton);
        }
        private void DgvProductEntries_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var dgv = (DataGridView)sender;
            if (dgv.Columns[e.ColumnIndex].Name == "DeleteButton")
            {
                var entry = (ProductEntry)dgv.Rows[e.RowIndex].DataBoundItem;
                if (MessageBox.Show("Bu girişi silmek istediğinizden emin misiniz?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _context.ProductEntries.Remove(entry);
                    _context.SaveChanges();
                    LoadData();
                }
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "EditButton")
            {
                var entry = (ProductEntry)dgv.Rows[e.RowIndex].DataBoundItem;
                using (var form = new Forms.ProductEntryForm(_context, entry.Id))
                {
                    form.ShowDialog();
                    LoadData();
                }
            }
        }
        private void DgvProductUsages_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var dgv = (DataGridView)sender;
            if (dgv.Columns[e.ColumnIndex].Name == "DeleteButton")
            {
                var usage = (ProductUsage)dgv.Rows[e.RowIndex].DataBoundItem;
                if (MessageBox.Show("Bu kullanım kaydını silmek istediğinizden emin misiniz?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _context.ProductUsages.Remove(usage);
                    _context.SaveChanges();
                    LoadData();
                }
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "EditButton")
            {
                var usage = (ProductUsage)dgv.Rows[e.RowIndex].DataBoundItem;
                using (var form = new Forms.ProductUsageForm(_context, usage.Id))
                {
                    form.ShowDialog();
                    LoadData();
                }
            }
        }
      
        private void ApplyFilters() { string category = cmbCategoryFilter.SelectedItem?.ToString() ?? "Tümü"; string level = cmbLevelFilter.SelectedItem?.ToString() ?? "Tümü"; if (mainTabControl.SelectedTab.Text != "Ürün Kullanımı") { level = "Tümü"; } LoadData(category, level); }
        private void BtnClearFilter_Click(object sender, EventArgs e) { cmbCategoryFilter.SelectedItem = "Tümü"; cmbLevelFilter.SelectedItem = "Tümü"; ApplyFilters(); }
        private void ToggleLevelFilter() { bool isUsageTab = mainTabControl.SelectedTab.Text == "Ürün Kullanımı"; cmbLevelFilter.Visible = isUsageTab; cmbLevelFilter.Parent.Controls.OfType<Label>().First(lbl => lbl.Text == "Kademe:").Visible = isUsageTab; }
        private void BtnExportToExcel_Click(object sender, EventArgs e) { if (mainTabControl.SelectedTab == null) return; DataGridView dgv = mainTabControl.SelectedTab.Controls.OfType<DataGridView>().FirstOrDefault(); if (dgv == null || dgv.Rows.Count == 0) { MessageBox.Show("Aktarılacak veri bulunamadı."); return; } SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV Dosyası (*.csv)|*.csv", Title = "Kaydet", FileName = $"{mainTabControl.SelectedTab.Text.Replace(" ", "_")}_Rapor_{DateTime.Now:yyyyMMdd}.csv" }; if (sfd.ShowDialog() == DialogResult.OK) { try { StringBuilder csv = new StringBuilder(); var headers = dgv.Columns.Cast<DataGridViewColumn>(); csv.AppendLine(string.Join(";", headers.Select(column => $"\"{column.HeaderText}\"").ToArray())); foreach (DataGridViewRow row in dgv.Rows) { if (row.IsNewRow) continue; var cells = row.Cells.Cast<DataGridViewCell>(); csv.AppendLine(string.Join(";", cells.Select(cell => $"\"{cell.Value}\"").ToArray())); } File.WriteAllText(sfd.FileName, csv.ToString(), Encoding.UTF8); MessageBox.Show("Veriler başarıyla aktarıldı!"); } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } } }
        private void DgvStockList_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) { if (e.RowIndex < 0) return; var dgv = sender as DataGridView; if (dgv.Columns["KalanMiktar"] == null) return; if (dgv.Columns[e.ColumnIndex].Name == "KalanMiktar") { if (e.Value == null || !int.TryParse(e.Value.ToString(), out int stockAmount)) return; if (stockAmount <= 0) { e.CellStyle.BackColor = Color.Red; e.CellStyle.ForeColor = Color.White; } else if (stockAmount <= 5) { e.CellStyle.BackColor = Color.Yellow; e.CellStyle.ForeColor = Color.Black; } else { e.CellStyle.BackColor = Color.White; e.CellStyle.ForeColor = Color.Black; } } }
        private void BtnAddCategory_Click(object sender, EventArgs e) { using (var form = new Forms.AddCategoryForm(_context)) { form.ShowDialog(); LoadFilterOptions(); LoadData(); } }
        private void BtnAddNewProduct_Click(object sender, EventArgs e) { using (var form = new Forms.ProductEntryForm(_context)) { form.ShowDialog(); LoadData(); } }
        private void BtnRemoveProduct_Click(object sender, EventArgs e) { using (var form = new Forms.ProductUsageForm(_context)) { form.ShowDialog(); LoadFilterOptions(); LoadData(); } }
        protected override void OnFormClosing(FormClosingEventArgs e) { base.OnFormClosing(e); _context?.Dispose(); }
        
    }
    
}
