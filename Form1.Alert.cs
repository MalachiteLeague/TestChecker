using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestChecker.Helpers;
using TestChecker.Models;
using TestChecker.Services;

namespace TestChecker
{
    // --- PHẦN 1: LOGIC CHÍNH CỦA FORM ---
    public partial class Form1
    {
        // 1. KHAI BÁO BIẾN (Tất cả dồn về đây)
        private FirebaseRepository<TestJob> _jobRepo;
        private BindingList<TestJob> _jobs = new BindingList<TestJob>();

        private readonly string[] _userList = new string[] {
            "HC_T1", "HC_T3", "HH_T1", "HH_T3", "SH_T1", "MD_T1", "SH_MDT3"
        };

        private string _myUsername;

        // Control giao diện
        ComboBox cboMyName, cboReceiver;
        TextBox txtContent, txtSID;
        ListBox lstLog;
        ContextMenuStrip ctxMenu;

        // Biến quản lý Alert
        private AlertForm _currentAlert = null;

        // 2. HÀM KHỞI ĐỘNG (Được gọi từ Form1.cs)
        private void KhoiDongUngDung()
        {
            _myUsername = _userList[0];

            SetupUI(); // Vẽ giao diện

            GlobalSyncService.Instance.Start();
            _jobRepo = new FirebaseRepository<TestJob>("test_jobs");

            this.Load += async (s, e) => await StartListening();
        }

        // 3. CÁC HÀM LOGIC & XỬ LÝ (Copy nguyên xi từ code cũ sang)
        private void Log(string message)
        {
            this.SafeInvoke(() => {
                string time = DateTime.Now.ToString("HH:mm:ss");
                lstLog.Items.Insert(0, $"[{time}] {message}");
                while (lstLog.Items.Count > 100) lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
            });
        }

        private async Task StartListening()
        {
            await _jobRepo.BindToGridAsync(this, _jobs);

            GlobalSyncService.Instance.Subscribe("test_jobs", (e) =>
            {
                try
                {
                    var newJob = e.ToObject<TestJob>();
                    if (string.IsNullOrEmpty(newJob.Id)) return;

                    bool dungNguoiNhan = newJob.Receiver == _myUsername;
                    bool dungTrangThai = newJob.Status == "PENDING";
                    bool khongPhaiToiGui = newJob.Sender != _myUsername;

                    if (dungNguoiNhan && dungTrangThai && khongPhaiToiGui)
                    {
                        Log($"🔔 CÓ VIỆC MỚI! SID: {newJob.Id}");
                        this.SafeInvoke(() => {
                            ShowLungLinhAlert(newJob);
                        });
                    }
                }
                catch { }
            }, null);
        }

        private void ShowLungLinhAlert(TestJob job)
        {
            if (_currentAlert != null && !_currentAlert.IsDisposed)
            {
                _currentAlert.Close();
                _currentAlert.Dispose();
            }
            _currentAlert = new AlertForm(job);
            _currentAlert.Show();
        }

        private void SetupUI()
        {
            this.Size = new Size(1000, 650);
            this.Font = new Font("Segoe UI", 10F);
            this.Text = "Hệ thống Xét Nghiệm Realtime (Admin Mode)";

            // Header
            new Label { Parent = this, Text = "Vị trí của tôi:", Top = 25, Left = 20, AutoSize = true };
            cboMyName = new ComboBox
            {
                Parent = this,
                Top = 22,
                Left = 130,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = new List<string>(_userList)
            };
            cboMyName.SelectedItem = _myUsername;
            cboMyName.SelectedIndexChanged += (s, e) => {
                _myUsername = cboMyName.SelectedItem.ToString();
                this.Text = $"User: {_myUsername}";
                Log($"Đổi vị trí sang: {_myUsername}");
            };

            // Khung Giao Việc
            var grpSend = new GroupBox { Parent = this, Text = "Tạo Yêu Cầu", Top = 70, Left = 20, Size = new Size(350, 260) };
            new Label { Parent = grpSend, Text = "Mã SID:", Top = 40, Left = 20, AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
            txtSID = new TextBox { Parent = grpSend, Top = 37, Left = 100, Width = 230, PlaceholderText = "Quét mã vạch..." };
            new Label { Parent = grpSend, Text = "Chỉ định:", Top = 90, Left = 20, AutoSize = true };
            txtContent = new TextBox { Parent = grpSend, Top = 87, Left = 100, Width = 230 };
            new Label { Parent = grpSend, Text = "Gửi đến:", Top = 140, Left = 20, AutoSize = true };
            cboReceiver = new ComboBox { Parent = grpSend, Top = 137, Left = 100, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = new List<string>(_userList) };
            if (_userList.Length > 1) cboReceiver.SelectedIndex = 1;
            var btnSend = new Button { Parent = grpSend, Text = "🚀 GỬI YÊU CẦU", Top = 200, Left = 100, Width = 230, Height = 40, BackColor = Color.LightSkyBlue, Cursor = Cursors.Hand };
            btnSend.Click += BtnSend_Click;

            // Khung Log & Reset
            var grpLog = new GroupBox { Parent = this, Text = "Nhật ký hệ thống", Top = 340, Left = 20, Size = new Size(350, 200) };
            lstLog = new ListBox { Parent = grpLog, Top = 20, Left = 10, Width = 330, Height = 130, BorderStyle = BorderStyle.None, BackColor = Color.Black, ForeColor = Color.Lime, Font = new Font("Consolas", 9F) };
            var btnReset = new Button { Parent = grpLog, Text = "🔥 RESET (Pass 9999)", Top = 160, Left = 10, Width = 330, Height = 30, BackColor = Color.IndianRed, ForeColor = Color.White };
            btnReset.Click += BtnReset_Click;

            // Lưới hiển thị
            dataGridView1.Parent = this;
            dataGridView1.Top = 70; dataGridView1.Left = 400;
            dataGridView1.Width = 560; dataGridView1.Height = 470;
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.BackgroundColor = Color.White;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.DataSource = _jobs;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.CellFormatting += DataGridView1_CellFormatting;

            // Context Menu
            ctxMenu = new ContextMenuStrip();
            var itemProcess = ctxMenu.Items.Add("👨‍🔧 Tiếp nhận xử lý");
            var itemDone = ctxMenu.Items.Add("✅ Trả kết quả");
            ctxMenu.Items.Add(new ToolStripSeparator());
            var itemDelete = ctxMenu.Items.Add("🗑️ Xóa phiếu này");
            itemDelete.ForeColor = Color.Red;
            itemProcess.Click += async (s, e) => await UpdateStatus("PROCESSING");
            itemDone.Click += async (s, e) => await ShowResultDialog();
            itemDelete.Click += async (s, e) => await DeleteSelectedJob();
            dataGridView1.ContextMenuStrip = ctxMenu;
        }

        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dataGridView1.Rows[e.RowIndex].DataBoundItem is TestJob job)
            {
                if (job.Status == "PROCESSING") dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
                else if (job.Status == "DONE") dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                else if (job.Status == "RETEST") dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightPink;
                else dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
            }
        }

        private async Task UpdateStatus(string newStatus)
        {
            if (dataGridView1.CurrentRow?.DataBoundItem is TestJob selectedJob)
            {
                selectedJob.Status = newStatus;
                await GlobalSyncService.Instance.UpdateDataAsync($"test_jobs/{selectedJob.Id}", selectedJob);
                Log($"Đổi trạng thái SID {selectedJob.Id} -> {newStatus}");
            }
        }

        private async Task ShowResultDialog()
        {
            if (!(dataGridView1.CurrentRow?.DataBoundItem is TestJob selectedJob)) return;

            Form prompt = new Form()
            {
                Width = 400,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = $"Trả kết quả SID: {selectedJob.Id}",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label lblText = new Label() { Left = 20, Top = 20, Text = "Nhập kết quả / ghi chú:", AutoSize = true };
            TextBox inputBox = new TextBox() { Left = 20, Top = 45, Width = 340, Text = "Hoàn thành" };
            Button btnYes = new Button() { Text = "✅ HOÀN THÀNH", Left = 20, Width = 160, Top = 90, Height = 40, DialogResult = DialogResult.Yes, BackColor = Color.LightGreen };
            Button btnNo = new Button() { Text = "❌ LẤY MẪU LẠI", Left = 200, Width = 160, Top = 90, Height = 40, DialogResult = DialogResult.No, BackColor = Color.LightPink };
            prompt.Controls.Add(lblText); prompt.Controls.Add(inputBox); prompt.Controls.Add(btnYes); prompt.Controls.Add(btnNo); prompt.AcceptButton = btnYes;

            DialogResult result = prompt.ShowDialog();
            if (result == DialogResult.Yes)
            {
                selectedJob.Status = "DONE";
                selectedJob.Result = string.IsNullOrWhiteSpace(inputBox.Text) ? "Hoàn thành" : inputBox.Text;
            }
            else if (result == DialogResult.No)
            {
                selectedJob.Status = "RETEST"; selectedJob.Result = "Yêu cầu lấy lại mẫu";
            }
            else return;

            await GlobalSyncService.Instance.UpdateDataAsync($"test_jobs/{selectedJob.Id}", selectedJob);
            Log($"Đã trả kết quả: {selectedJob.Result}");
        }

        private async Task DeleteSelectedJob()
        {
            if (!(dataGridView1.CurrentRow?.DataBoundItem is TestJob selectedJob)) return;
            if (selectedJob.Sender != _myUsername) { MessageBox.Show("Không có quyền!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show($"Xóa SID: {selectedJob.Id}?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                await GlobalSyncService.Instance.DeleteDataAsync($"test_jobs/{selectedJob.Id}");
                Log($"Đã xóa phiếu SID: {selectedJob.Id}");
            }
        }

        private async void BtnReset_Click(object sender, EventArgs e)
        {
            string inputPass = Microsoft.VisualBasic.Interaction.InputBox("Nhập mật khẩu Admin:", "CẢNH BÁO", "");
            if (inputPass == "9999") { await GlobalSyncService.Instance.DeleteDataAsync("test_jobs"); Log("!!! RESET TOÀN BỘ !!!"); }
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            string sid = txtSID.Text.Trim().ToUpper();
            string content = txtContent.Text.Trim();
            string targetUser = cboReceiver.SelectedItem.ToString();
            if (targetUser == _myUsername) { MessageBox.Show("Không thể tự gửi cho mình!"); return; }
            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(content)) { MessageBox.Show("Thiếu thông tin!"); return; }

            var job = new TestJob { Id = sid, Sender = _myUsername, Receiver = targetUser, Content = content, Status = "PENDING", Result = "N/A" };
            try
            {
                await GlobalSyncService.Instance.UpdateDataAsync($"test_jobs/{sid}", job);
                Log($"Đã gửi: SID={sid} -> {targetUser}");
                txtSID.Clear(); txtContent.Clear(); txtSID.Focus();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }
    }

    // --- PHẦN 2: CLASS ALERT FORM (Để chung ở đây luôn cho gọn) ---
    public class AlertForm : Form
    {
        private System.Windows.Forms.Timer _blinkTimer;
        private bool _isRed = true;

        public AlertForm(TestJob job)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(700, 450);
            this.TopMost = true;
            this.BackColor = Color.Red;
            this.Padding = new Padding(5);

            Button btnOK = new Button
            {
                Parent = this,
                Text = "TÔI ĐÃ THẤY (CLOSE)",
                Dock = DockStyle.Bottom,
                Height = 70,
                Font = new Font("Arial", 16, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.Black,
                Cursor = Cursors.Hand
            };
            btnOK.Click += (s, e) => this.Close();

            Label lblTitle = new Label
            {
                Parent = this,
                Text = "🚨 CÓ YÊU CẦU XÉT NGHIỆM MỚI! 🚨",
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.Yellow,
                BackColor = Color.Transparent
            };

            Panel pnlContent = new Panel { Parent = this, Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(10) };

            Label lblInfo = new Label
            {
                Parent = pnlContent,
                Text = $"SID: {job.Id}\r\n\r\nTừ: {job.Sender}\r\n\r\nNội dung: {job.Content}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true
            };
            lblInfo.BringToFront();

            _blinkTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _blinkTimer.Tick += (s, e) => {
                if (_isRed) { this.BackColor = Color.Gold; lblTitle.ForeColor = Color.Red; lblInfo.ForeColor = Color.Black; }
                else { this.BackColor = Color.Red; lblTitle.ForeColor = Color.Yellow; lblInfo.ForeColor = Color.White; }
                _isRed = !_isRed;
            };
            _blinkTimer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_blinkTimer != null) { _blinkTimer.Stop(); _blinkTimer.Dispose(); _blinkTimer = null; }
            base.OnFormClosed(e);
        }
    }
}