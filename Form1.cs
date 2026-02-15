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
    public partial class Form1 : Form
    {
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

        public Form1()
        {
            InitializeComponent();
            _myUsername = _userList[0];

            SetupUI();

            GlobalSyncService.Instance.Start();
            _jobRepo = new FirebaseRepository<TestJob>("test_jobs");

            this.Load += async (s, e) => await StartListening();
        }

        private void Log(string message)
        {
            this.SafeInvoke(() => {
                string time = DateTime.Now.ToString("HH:mm:ss");

                // 1. Chèn dòng mới
                lstLog.Items.Insert(0, $"[{time}] {message}");

                // 2. TỐI ƯU HÓA: Chỉ giữ lại 100 dòng log gần nhất
                // Nếu quá 100 dòng thì xóa bớt dòng cũ ở cuối đi
                while (lstLog.Items.Count > 100)
                {
                    lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
                }
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
                            MessageBox.Show(
                                new Form { TopMost = true },
                                $"Mã SID: {newJob.Id}\nChỉ định: {newJob.Content}",
                                $"YÊU CẦU TỪ {newJob.Sender}",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                    }
                }
                catch { }
            }, null);
        }

        private void SetupUI()
        {
            this.Size = new Size(1000, 650);
            this.Font = new Font("Segoe UI", 10F);
            this.Text = "Hệ thống Xét Nghiệm Realtime (Admin Mode)";

            // 1. HEADER
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

            // 2. KHUNG GIAO VIỆC
            var grpSend = new GroupBox
            {
                Parent = this,
                Text = "Tạo Yêu Cầu",
                Top = 70,
                Left = 20,
                Size = new Size(350, 260)
            };

            new Label { Parent = grpSend, Text = "Mã SID:", Top = 40, Left = 20, AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
            txtSID = new TextBox { Parent = grpSend, Top = 37, Left = 100, Width = 230, PlaceholderText = "Quét mã vạch..." };

            new Label { Parent = grpSend, Text = "Chỉ định:", Top = 90, Left = 20, AutoSize = true };
            txtContent = new TextBox { Parent = grpSend, Top = 87, Left = 100, Width = 230 };

            new Label { Parent = grpSend, Text = "Gửi đến:", Top = 140, Left = 20, AutoSize = true };
            cboReceiver = new ComboBox
            {
                Parent = grpSend,
                Top = 137,
                Left = 100,
                Width = 230,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = new List<string>(_userList)
            };
            if (_userList.Length > 1) cboReceiver.SelectedIndex = 1;

            var btnSend = new Button
            {
                Parent = grpSend,
                Text = "🚀 GỬI YÊU CẦU",
                Top = 200,
                Left = 100,
                Width = 230,
                Height = 40,
                BackColor = Color.LightSkyBlue,
                Cursor = Cursors.Hand
            };
            btnSend.Click += BtnSend_Click;

            // 3. KHUNG LOG & NÚT RESET (MỚI)
            var grpLog = new GroupBox { Parent = this, Text = "Nhật ký hệ thống", Top = 340, Left = 20, Size = new Size(350, 200) };
            lstLog = new ListBox { Parent = grpLog, Top = 20, Left = 10, Width = 330, Height = 130, BorderStyle = BorderStyle.None, BackColor = Color.Black, ForeColor = Color.Lime, Font = new Font("Consolas", 9F) };

            // --- NÚT XÓA TRẮNG HỆ THỐNG ---
            var btnReset = new Button
            {
                Parent = grpLog,
                Text = "🔥 RESET TOÀN BỘ (Pass 9999)",
                Top = 160,
                Left = 10,
                Width = 330,
                Height = 30,
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnReset.Click += BtnReset_Click;

            // 4. LƯỚI HIỂN THỊ
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

            // --- MENU CHUỘT PHẢI CẬP NHẬT MỚI ---
            ctxMenu = new ContextMenuStrip();

            // Nhóm 1: Thao tác Xử lý
            var itemProcess = ctxMenu.Items.Add("👨‍🔧 Tiếp nhận xử lý");
            var itemDone = ctxMenu.Items.Add("✅ Trả kết quả");
            ctxMenu.Items.Add(new ToolStripSeparator()); // Đường kẻ ngang phân cách

            // Nhóm 2: Thao tác Admin
            var itemDelete = ctxMenu.Items.Add("🗑️ Xóa phiếu này");
            itemDelete.ForeColor = Color.Red; // Tô đỏ cho nguy hiểm

            itemProcess.Click += async (s, e) => await UpdateStatus("PROCESSING");
            itemDone.Click += async (s, e) => await ShowResultDialog();
            itemDelete.Click += async (s, e) => await DeleteSelectedJob(); // Gọi hàm xóa

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

        // --- TÍNH NĂNG 1: XÓA PHIẾU (Chỉ Sender mới xóa được) ---
        private async Task DeleteSelectedJob()
        {
            if (!(dataGridView1.CurrentRow?.DataBoundItem is TestJob selectedJob)) return;

            // Kiểm tra quyền
            if (selectedJob.Sender != _myUsername)
            {
                MessageBox.Show($"Bạn không phải người tạo phiếu này!\nNgười tạo là: {selectedJob.Sender}", "Không có quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Bạn có chắc muốn xóa phiếu SID: {selectedJob.Id}?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                await GlobalSyncService.Instance.DeleteDataAsync($"test_jobs/{selectedJob.Id}");
                Log($"Đã xóa phiếu SID: {selectedJob.Id}");
            }
        }

        // --- TÍNH NĂNG 2: RESET TOÀN BỘ (Pass 9999) ---
        private async void BtnReset_Click(object sender, EventArgs e)
        {
            // Hỏi mật khẩu
            string inputPass = Microsoft.VisualBasic.Interaction.InputBox("Nhập mật khẩu Admin để xóa toàn bộ dữ liệu:", "CẢNH BÁO NGUY HIỂM", "");

            if (inputPass == "9999")
            {
                await GlobalSyncService.Instance.DeleteDataAsync("test_jobs"); // Xóa sạch thư mục test_jobs
                MessageBox.Show("Đã dọn sạch hệ thống!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log("!!! HỆ THỐNG ĐÃ RESET TOÀN BỘ !!!");
            }
            else if (!string.IsNullOrEmpty(inputPass))
            {
                MessageBox.Show("Sai mật khẩu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
}