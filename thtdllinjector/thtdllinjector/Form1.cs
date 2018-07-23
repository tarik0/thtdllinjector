using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace thtdllinjector
{
    public partial class Form1 : Form
    {

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public static string filePath = "";

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress,
            IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern void FreeLibraryAndExitThread(IntPtr hModule, uint dwExitCode);



        // privileges
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;
        public IntPtr pHandle = IntPtr.Zero;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        public IntPtr InjectDLL(Process target, string dllName)
        {
            try
            {
                IntPtr pHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, target.Id);
                if (pHandle == null && pHandle != IntPtr.Zero)
                {
                    throw new System.Exception("Cannot open the process.");
                }
                listBox1.Items.Add("Enjekte Başlanıyor!");
                listBox1.Items.Add("Process Handle Alındı");

                IntPtr loadLibAdd = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibAdd == null && loadLibAdd != IntPtr.Zero)
                {
                    throw new System.Exception("Cannot find LoadLibraryA Process Address.");
                }
                listBox1.Items.Add("LoadLibraryA Adresi Bulundu");

                IntPtr allocMemAdd = VirtualAllocEx(pHandle, IntPtr.Zero, (uint)((dllName.Length + 1)), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (allocMemAdd == null && allocMemAdd != IntPtr.Zero)
                {
                    throw new System.Exception("Cannot VirtualAllocEx.");
                }
                listBox1.Items.Add("Yazılıyor");

                UIntPtr bytesWritten;
                bool result = WriteProcessMemory(pHandle, allocMemAdd, Encoding.Default.GetBytes(dllName), (uint)((dllName.Length + 1)), out bytesWritten);
                if (result == false)
                {
                    throw new System.Exception("Cannot write process memory.");
                }

                //IntPtr threadHandle = CreateRemoteThread(pHandle, IntPtr.Zero, 0, loadLibAdd, allocMemAdd, 0, IntPtr.Zero);
                //IntPtr)CreateRemoteThread(hProcess, (IntPtr)null, 0, Injector, AllocMem, 0, out bytesout);
                // CreateRemoteThread(pHandle,IntPtr.Zero, 0,loadLibAdd, allocMemAdd)
                IntPtr bytesWriten1;
                IntPtr threadHandle = CreateRemoteThread(pHandle, IntPtr.Zero, 0, loadLibAdd, allocMemAdd, 0, out bytesWriten1);
                if (threadHandle != null && threadHandle != IntPtr.Zero)
                {
                    listBox1.Items.Add("Başarılı");
                    CloseHandle(pHandle);
                    return threadHandle;
                }
                else
                {
                    throw new System.Exception("Cannot create a remote thread.");
                }
            }
            catch (System.Exception ex)
            {
                listBox1.Items.Add("Hata!: " + ex.Message);
                return IntPtr.Zero;
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            comboBox1.SelectedIndex = 0;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            file.Filter = "DLL Dosyası | *.dll";
            file.RestoreDirectory = true;
            file.ShowDialog();
            filePath = file.FileName;
            textBox2.Text = file.SafeFileName;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form2 second = new Form2();
            second.SendForm(ref textBox1);
            second.Show();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "")
            {
                MessageBox.Show("Lütfen bütün boşlukları doldurunuz!");
            } else
            {
                try
                {
                    Process selectedProcess = Process.GetProcessById(Int32.Parse(textBox1.Text));
                    IntPtr handle = InjectDLL(selectedProcess,filePath);
                    if (handle != IntPtr.Zero)
                    {
                        MessageBox.Show("DLL Enjektesi Başarılı!");
                    } else
                    {
                        MessageBox.Show("DLL Enjektesi Başarısız!");
                    }
                } catch (Exception ex)
                {
                    MessageBox.Show("DLL Enjektesi Başarısız: " + ex.Message);
                }
            }
        }

        private void label1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}
