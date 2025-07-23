// Forms/ProductUsageForm.cs

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DepoTakip.DataAccess;
using DepoTakip.Models;

namespace DepoTakip.Forms
{
    public class ProductUsageForm : Form
    {
        private readonly DatabaseContext _context;
        private ComboBox cmbProduct, cmbUserLevel;
        private TextBox txtUsedBy;
        private NumericUpDown numQuantity;
        private Button btnSave;
        private Label lblStock;

        // Düzenlenecek kaydın ID'sini tutmak için.
        private int? _usageId;
        
        public ProductUsageForm(DatabaseContext context) : this(context, null)
        {
        }

    
public ProductUsageForm(DatabaseContext context, int? usageId)
{
    string basePath = AppDomain.CurrentDomain.BaseDirectory;
string iconPath = Path.Combine(basePath, "Resources", "uguricon.ico");
this.Icon = new Icon(iconPath);
    _context = context;
    _usageId = usageId;

    this.Text = _usageId.HasValue ? "Ürün Kullanımını Düzenle" : "Yeni Ürün Kullanımı";
    this.Size = new Size(420, 300);
    this.StartPosition = FormStartPosition.CenterParent;
    this.FormBorderStyle = FormBorderStyle.FixedDialog; // Boyut sabit
    this.BackColor = Color.WhiteSmoke;

    InitializeControls();
    LoadProducts();

    if (_usageId.HasValue)
        LoadUsageData();
}

private void InitializeControls()
{
    // Tema renkleri
    Color bgColor = Color.FromArgb(245, 250, 255);
    Color btnMain = Color.FromArgb(123, 210, 255);
    Color btnText = Color.Black;
    Color lblMain = Color.FromArgb(39, 87, 130);

    this.BackColor = bgColor;

    Font labelFont = new Font("Segoe UI", 10, FontStyle.Bold);
    Font inputFont = new Font("Segoe UI", 10);

    int labelX = 18, inputX = 150, spaceY = 40, curY = 20; 

  
    var lblProduct = new Label { Text = "Kullanılan Ürün:", Location = new Point(labelX, curY), AutoSize = true, Font = labelFont, ForeColor = lblMain };
    cmbProduct = new ComboBox { Location = new Point(inputX, curY - 2), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = inputFont, ForeColor = Color.Black };
    cmbProduct.SelectedIndexChanged += CmbProduct_SelectedIndexChanged;
    this.Controls.Add(lblProduct);
    this.Controls.Add(cmbProduct);

  
    lblStock = new Label
    {
        Text = "Stok: -",
        Location = new Point(inputX + 165, curY), 
        ForeColor = Color.SeaGreen,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        AutoSize = true
    };
    this.Controls.Add(lblStock);

    curY += spaceY;
    var lblUsedBy = new Label { Text = "Kullanan Kişi:", Location = new Point(labelX, curY), AutoSize = true, Font = labelFont, ForeColor = lblMain };
    txtUsedBy = new TextBox { Location = new Point(inputX, curY - 2), Width = 200, Font = inputFont };
    this.Controls.Add(lblUsedBy);
    this.Controls.Add(txtUsedBy);

    curY += spaceY;
    var lblLevel = new Label { Text = "Kademe:", Location = new Point(labelX, curY), AutoSize = true, Font = labelFont, ForeColor = lblMain };
    cmbUserLevel = new ComboBox { Location = new Point(inputX, curY - 2), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = inputFont };
    cmbUserLevel.Items.AddRange(new object[] { "İlkokul", "Ortaokul", "Lise", "Yönetim", "Diğer" });
    this.Controls.Add(lblLevel);
    this.Controls.Add(cmbUserLevel);

    curY += spaceY;
    var lblQty = new Label { Text = "Kullanılan Miktar:", Location = new Point(labelX, curY), AutoSize = true, Font = labelFont, ForeColor = lblMain };
    numQuantity = new NumericUpDown { Location = new Point(inputX, curY - 2), Width = 150, Minimum = 1, Maximum = 1000, Font = inputFont };
    this.Controls.Add(lblQty);
    this.Controls.Add(numQuantity);

  
    btnSave = new Button
    {
        Text = "Kaydet",
        Location = new Point(this.ClientSize.Width - 140, this.ClientSize.Height - 60),
        Size = new Size(110, 36),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        BackColor = btnMain,
        ForeColor = btnText,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat
    };
    btnSave.FlatAppearance.BorderColor = Color.LightGray;
    btnSave.FlatAppearance.BorderSize = 1;
    btnSave.Click += BtnSave_Click;
    this.Controls.Add(btnSave);

    // Pencere boyutu sabit
    this.FormBorderStyle = FormBorderStyle.FixedDialog;
    this.MaximizeBox = false;
    this.MinimizeBox = false;
}

// Stok Label Renk ve Yazısı
private void UpdateStockLabel(int currentStock)
{
    if (currentStock <= 0)
    {
        lblStock.Text = "Stok: 0 (Stok kalmadı!)";
        lblStock.ForeColor = Color.Red;
    }
    else if (currentStock <= 5)
    {
        lblStock.Text = $"Stok: {currentStock} (Azaldı!)";
        lblStock.ForeColor = Color.OrangeRed;
    }
    else
    {
        lblStock.Text = $"Stok: {currentStock}";
        lblStock.ForeColor = Color.SeaGreen;
    }
}

        private void LoadProducts()
        {
            var stock = _context.ProductEntries.GroupBy(p => new { p.ProductName }).Select(g => new { ProductName = g.Key.ProductName, TotalEntry = g.Sum(p => p.Quantity) }).ToList();
            var usage = _context.ProductUsages.GroupBy(u => u.ProductName).Select(g => new { ProductName = g.Key, TotalUsage = g.Sum(u => u.Quantity) }).ToList();
            var availableProducts = from s in stock join u in usage on s.ProductName equals u.ProductName into usageGroup from ug in usageGroup.DefaultIfEmpty() where s.TotalEntry - (ug?.TotalUsage ?? 0) > 0 select s.ProductName;
            cmbProduct.DataSource = availableProducts.Distinct().ToList();
        }

       
 private void LoadUsageData()
{
    var usage = _context.ProductUsages.Find(_usageId.Value);
    if (usage != null)
    {
        cmbProduct.DataSource = new[] { usage.ProductName };
        cmbProduct.SelectedItem = usage.ProductName;
        cmbProduct.Enabled = false;

        txtUsedBy.Text = usage.UsedBy;
        cmbUserLevel.SelectedItem = usage.UserLevel;

        int totalEntry = _context.ProductEntries.Where(p => p.ProductName == usage.ProductName).Sum(p => p.Quantity);
        int totalUsage = _context.ProductUsages.Where(u => u.ProductName == usage.ProductName).Sum(u => u.Quantity);

        int realCurrentStock = totalEntry - totalUsage;

     
        int maxUsage = realCurrentStock + usage.Quantity;
        if (maxUsage < 1)
            maxUsage = usage.Quantity; 

        numQuantity.Minimum = 1;
        numQuantity.Maximum = maxUsage;

       
        if (realCurrentStock <= 0)
        {
            lblStock.Text = "Stok: 0 (Stok kalmadı!)";
            lblStock.ForeColor = Color.Red;
        }
        else
        {
            lblStock.Text = $"Stok: {realCurrentStock}";
            lblStock.ForeColor = Color.Blue;
        }

        

        decimal valueToSet = usage.Quantity;
        if (valueToSet < numQuantity.Minimum) valueToSet = numQuantity.Minimum;
        if (valueToSet > numQuantity.Maximum) valueToSet = numQuantity.Maximum;
        numQuantity.Value = valueToSet;
    }
}




        private void CmbProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbProduct.SelectedItem == null) return;
            string selectedProduct = cmbProduct.SelectedItem.ToString();
            int totalEntry = _context.ProductEntries.Where(p => p.ProductName == selectedProduct).Sum(p => p.Quantity);
            int totalUsage = _context.ProductUsages.Where(u => u.ProductName == selectedProduct).Sum(u => u.Quantity);
            int currentStock = totalEntry - totalUsage;
            lblStock.Text = $"Stok: {currentStock}";
            numQuantity.Maximum = currentStock > 0 ? currentStock : 1;
        }

 
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cmbProduct.SelectedItem == null || string.IsNullOrWhiteSpace(txtUsedBy.Text) || cmbUserLevel.SelectedItem == null)
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.", "Hata");
                return;
            }

            if (_usageId.HasValue) // Düzenleme Modu
            {
                var usageToUpdate = _context.ProductUsages.Find(_usageId.Value);
                if (usageToUpdate != null)
                {
                    usageToUpdate.UsedBy = txtUsedBy.Text.Trim();
                    usageToUpdate.UserLevel = cmbUserLevel.SelectedItem.ToString();
                    usageToUpdate.Quantity = (int)numQuantity.Value;
                    _context.ProductUsages.Update(usageToUpdate);
                    MessageBox.Show("Kullanım kaydı başarıyla güncellendi.", "Başarılı");
                }
            }
            else // Yeni Kayıt Modu
            {
                var productInfo = _context.ProductEntries.First(p => p.ProductName == cmbProduct.SelectedItem.ToString());
                var usage = new ProductUsage
                {
                    ProductName = productInfo.ProductName,
                    Brand = productInfo.Brand,
                    CategoryName = productInfo.CategoryName,
                    UsedBy = txtUsedBy.Text.Trim(),
                    UserLevel = cmbUserLevel.SelectedItem.ToString(),
                    Quantity = (int)numQuantity.Value,
                    UsageDate = DateTime.Now
                };
                _context.ProductUsages.Add(usage);
                MessageBox.Show("Ürün kullanımı başarıyla kaydedildi.", "Başarılı");
            }

            _context.SaveChanges();
            this.Close();
        }
    }
}