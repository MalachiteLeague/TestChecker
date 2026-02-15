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
        ListBox lstLog; // <--- MỚI THÊM: Cái bảng đen ghi nhật ký

        public Form1()
        {
            InitializeComponent();
            _myUsername = _userList[0];

            SetupUI();

            GlobalSyncService.Instance.Start();
            _jobRepo = new FirebaseRepository<TestJob>("test_jobs");

            this.Load += async (s, e) => await StartListening();
        }

        // --- HÀM LOG: Ghi lại mọi hoạt động ---
        private void Log(string message)
        {
            this.SafeInvoke(() => {
                string time = DateTime.Now.ToString("HH:mm:ss");
                lstLog.Items.Insert(0, $"[{time}] {message}");
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

                    // DEBUG: In ra xem App đang nhận được cái gì
                    // Dòng này cực quan trọng để tìm lỗi
                    Log($"Nhận tin: SID={newJob.Id} | Gửi tới={newJob.Receiver}");

                    // Kiểm tra từng điều kiện một
                    bool dungNguoiNhan = newJob.Receiver == _myUsername;
                    bool dungTrangThai = newJob.Status == "PENDING";
                    bool khongPhaiToiGui = newJob.Sender != _myUsername;

                    if (dungNguoiNhan && dungTrangThai && khongPhaiToiGui)
                    {
                        Log("=> ĐÚNG LÀ VIỆC CỦA TÔI RỒI! BÁO ĐỘNG!");

                        this.SafeInvoke(() => {
                            // Dùng MessageBox kiểu này để nó luôn nổi lên trên cùng (TopMost)
                            MessageBox.Show(
                                new Form { TopMost = true },
                                $"Mã SID: {newJob.Id}\nChỉ định: {newJob.Content}",
                                $"🔔 CÓ VIỆC TỪ {newJob.Sender}",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        });
                    }
                    else
                    {
                        // Nếu không báo, in ra lý do tại sao không báo
                        if (!dungNguoiNhan) Log($"=> Bỏ qua: Vì tôi là {_myUsername} mà tin này gửi cho {newJob.Receiver}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Lỗi: {ex.Message}");
                }
            }, null);
        }

        private void SetupUI()
        {
            this.Size = new Size(900, 650); // Cao thêm chút để chứa log
            this.Font = new Font("Segoe UI", 10F);
            this.Text = "Hệ thống Giao Việc Xét Nghiệm Realtime";

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
                this.Text = $"Đang đăng nhập tại: {_myUsername}";
                Log($"Đã chuyển vị trí sang: {_myUsername}");
            };

            // 2. KHUNG GIAO VIỆC
            var grpSend = new GroupBox
            {
                Parent = this,
                Text = "Tạo Yêu Cầu Xét Nghiệm",
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

            // 3. KHUNG NHẬT KÝ (MỚI)
            var grpLog = new GroupBox
            {
                Parent = this,
                Text = "Nhật ký hệ thống (Debug)",
                Top = 340,
                Left = 20,
                Size = new Size(350, 200)
            };
            lstLog = new ListBox
            {
                Parent = grpLog,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.Black,
                ForeColor = Color.Lime, // Giao diện Hacker
                Font = new Font("Consolas", 9F)
            };

            // 4. LƯỚI HIỂN THỊ
            dataGridView1.Parent = this;
            dataGridView1.Top = 70; dataGridView1.Left = 400;
            dataGridView1.Width = 460; dataGridView1.Height = 470;
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.BackgroundColor = Color.White;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.DataSource = _jobs;
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            string sid = txtSID.Text.Trim().ToUpper();
            string content = txtContent.Text.Trim();
            string targetUser = cboReceiver.SelectedItem.ToString();

            if (targetUser == _myUsername) { MessageBox.Show("Không thể tự gửi cho mình!"); return; }
            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(content)) { MessageBox.Show("Thiếu thông tin!"); return; }

            var job = new TestJob
            {
                Id = sid,
                Sender = _myUsername,
                Receiver = targetUser,
                Content = content,
                Status = "PENDING",
                Result = "N/A"
            };

            try
            {
                await GlobalSyncService.Instance.UpdateDataAsync($"test_jobs/{sid}", job);
                Log($"Đã gửi lệnh cho {targetUser}: SID={sid}");

                txtSID.Clear(); txtContent.Clear(); txtSID.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}");
            }
        }
    }
}