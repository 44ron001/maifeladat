using System.Text.Json;

namespace WinFormsApp10
{
    public partial class Form1 : Form
    {
        public ListView listView = new ListView();
        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.MultiSelect = false;
            listView.HideSelection = false;
            listView.Font = new Font("Segoe UI", 10);
            listView.BackColor = Color.White;
            listView.ForeColor = Color.Black;
            listView.BorderStyle = BorderStyle.FixedSingle;
            listView.Dock = DockStyle.Fill;
            listView.MouseClick += ListView_MouseClick;
            

            Button add = new Button();
            add.Text = "+";
            add.Dock = DockStyle.Bottom;
            add.Click += Add_Click;
            this.Controls.Add(add);
            this.Controls.Add(listView);

            
            Reset();
        }

        private async void Add_Click(object? sender, EventArgs e)
        {
            string input = AdatHozzaado("Formátum:\nid,Column 1,Column 2,Column 3,Column 4", "Adat hozzáadó");
            if (string.IsNullOrWhiteSpace(input)) return;

            string[] values = input.Split(',');

            // Build JSON dynamically using the current column headers
            Dictionary<string, object> jsonData = new Dictionary<string, object>();
            for (int i = 0; i < listView.Columns.Count && i < values.Length; i++)
            {
                string key = listView.Columns[i].Text;
                jsonData[key] = values[i];
            }

            await UploadToApiAsync(jsonData);
            Reset();
        }



        public static async Task UploadToApiAsync(Dictionary<string, object> data)
        {
            using HttpClient client = new HttpClient();
            string url = "https://api-generator.retool.com/bpRxPt/data";
            string json = JsonSerializer.Serialize(data);
            StringContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Hiba a feltöltésnél! Status: {response.StatusCode}");
            }
        }




        private async void Reset()
        {
            this.UseWaitCursor = true;
            string[] csvRows = await FetchApiAsCsvRows("https://retoolapi.dev/bpRxPt/data");
            this.UseWaitCursor = false;
            listView.Items.Clear();
            listView.Columns.Clear();

            listView.Columns.Add("id", 60, HorizontalAlignment.Left);
            listView.Columns.Add("Column 1", 180, HorizontalAlignment.Left);
            listView.Columns.Add("Column 2", 80, HorizontalAlignment.Center);
            listView.Columns.Add("Column 3", 150, HorizontalAlignment.Left);
            listView.Columns.Add("Column 4", 80, HorizontalAlignment.Center);

            foreach (string d in csvRows)
            {
                string[] a = d.Split(',');
                ListViewItem item = new ListViewItem(a[0]);
                for (int i = 1; i < a.Length; i++)
                {
                    item.SubItems.Add(a[i]);
                }
                listView.Items.Add(item);
            }

        }

        private async void ListView_MouseClick(object? sender, MouseEventArgs e) {
            ListView lv = sender as ListView;
            if (lv == null || lv.SelectedItems.Count == 0) { return; }
            string firstColumnText = lv.SelectedItems[0].Text;
            int id = Convert.ToInt32(firstColumnText);
            string all = "";
            foreach (ListViewItem item in lv.SelectedItems) {
                List<string> cells = new List<string>();
                foreach (ListViewItem.ListViewSubItem sub in item.SubItems) {
                    cells.Add(sub.Text);
                }
                all += string.Join(",", cells) + Environment.NewLine;
            }
            string result = AdatSzerkeszto("Szerkessze meg a kívánt adatot:", "Adat szerkesztő / törlő", all, id);
            if (result == "DELETE" || result == "") {
                await DeleteFromApiAsync(id);
                Reset();
            } else {
                string[] newValues = result.Split(',');
                Dictionary<string, object> jsonData = new Dictionary<string, object>();
                for (int i = 0; i < lv.Columns.Count && i < newValues.Length; i++) {
                    string key = lv.Columns[i].Text;
                    jsonData[key] = newValues[i];
                }
                await UpdateApiAsync(id, jsonData);
                Reset();
            }
        }

        public static async Task UpdateApiAsync(int id, Dictionary<string, object> data) {
            using HttpClient client = new HttpClient();
            string url = $"https://api-generator.retool.com/bpRxPt/data/{id}";
            string json = JsonSerializer.Serialize(data);
            Clipboard.SetText(json);
            StringContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PutAsync(url, content);
            if (!response.IsSuccessStatusCode) {
                MessageBox.Show($"Hiba a frissítésnél! Status: {response.StatusCode}");
            }
        }

        public static string AdatSzerkeszto(string text, string caption, string original, int id)
        {
            Form prompt = new Form() { Width = 400, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog, Text = caption, StartPosition = FormStartPosition.CenterScreen };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true };
            TextBox inputBox = new TextBox() { Left = 20, Top = 50, Width = 340 };
            Button confirmation = new Button() { Text = "OK", Left = 280, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            Button deletion = new Button() { Text = "Törlés", Left = 190, Width = 80, Top = 80, DialogResult = DialogResult.Abort };
            confirmation.Click += (sender, e) => prompt.Close();
            deletion.Click += (sender, e) => prompt.Close();
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(inputBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(deletion);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = deletion;
            inputBox.Text = original;

            DialogResult result = prompt.ShowDialog();

            if (result == DialogResult.OK)
            {
                return (inputBox.Text);
            }
            else if (result == DialogResult.Abort)
            {
                return ("DELETE");
            }

            return ("");
        }


        public static string AdatHozzaado(string text, string caption)
        {
            Form prompt = new Form() { Width = 400, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog, Text = caption, StartPosition = FormStartPosition.CenterScreen };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true };
            TextBox inputBox = new TextBox() { Left = 20, Top = 50, Width = 340 };
            Button confirmation = new Button() { Text = "OK", Left = 280, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => prompt.Close();
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(inputBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;
 

            DialogResult result = prompt.ShowDialog();
            return (inputBox.Text);
        }

        public static async Task DeleteFromApiAsync(int id)
        {
            using HttpClient client = new HttpClient();
            string url = $"https://api-generator.retool.com/bpRxPt/data/{id}";
            HttpResponseMessage response = await client.DeleteAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Hiba a törlésnél! Status: {response.StatusCode}");
            }
        }

        public static async Task<string[]> FetchApiAsCsvRows(string url)
        {
            using HttpClient client = new HttpClient();
            string json = await client.GetStringAsync(url);
            JsonDocument doc = JsonDocument.Parse(json);
            List<string> rows = new List<string>();
            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                List<string> values = new List<string>();
                foreach (JsonProperty prop in element.EnumerateObject())
                {
                    values.Add(prop.Value.ToString());
                }
                rows.Add(string.Join(",", values));
            }
            return (rows.ToArray());
        }

        private void listView1_ItemActivate(object sender, EventArgs e)
        {

        }
    }
}