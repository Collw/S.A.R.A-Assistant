using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace welabs
{
    public partial class frmInfo : Form
    {
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private PerformanceCounter gpuCounter; // Adiciona o contador para GPU.
        private bool isInternetAvailable = false;
        private string providerName = "No Connection";

        public frmInfo()
        {
            InitializeComponent();
            InitializeCounters();
            UpdateSystemInfo();
            GetSystemInformation();

            // Configura o Timer para atualizar as informações a cada segundo.
            timerUpdate.Interval = 1000;
            timerUpdate.Tick += TimerUpdate_Tick;
            timerUpdate.Start();

            // Registra o evento para verificar a disponibilidade da conexão.
          
        }

        private void InitializeCounters()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            gpuCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_Total"); // Inicializa o contador para GPU.
        }

        private async void TimerUpdate_Tick(object sender, EventArgs e)
        {
            UpdateSystemInfo();
            CheckHighUsage();
            UpdateGPUUsage();
            // Verifica a conexão com a internet sem usar threads.
            UpdateNetworkInfo();
            await CheckInternetConnection();
        }

        private void UpdateSystemInfo()
        {
            float cpuUsage = cpuCounter.NextValue();
            float ramAvailable = ramCounter.NextValue();
            float ramTotal = GetTotalRAM();
            float ramUsed = ramTotal - ramAvailable;

            labelCPU.Text = $"{cpuUsage:F1}%";
            labelRAM.Text = $"{ramUsed:F1}MB / {ramTotal:F1}MB";
            
        }

        private float GetTotalRAM()
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024f * 1024f);
        }

        private void CheckHighUsage()
        {
            // Aguarda um pequeno atraso (200ms) entre as chamadas para obter valores mais precisos.
            System.Threading.Thread.Sleep(200);

            float cpuUsage = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(200); // Outro atraso antes da segunda leitura.
            cpuUsage = cpuCounter.NextValue();

            float ramAvailable = ramCounter.NextValue();
            float ramTotal = GetTotalRAM();
            float ramUsed = ramTotal - ramAvailable;

            Color limeGreenLight = Color.FromArgb(0, 255, 0); // Verde claro personalizado

            if (cpuUsage > 80)
            {
                labelCPU.ForeColor = Color.Red;
            }
            else
            {
                labelCPU.ForeColor = limeGreenLight;
            }

            if (ramUsed / ramTotal * 100 > 80)
            {
                labelRAM.ForeColor = Color.Red;
            }
            else
            {
                labelRAM.ForeColor = limeGreenLight;
            }
        }

        private void GetSystemInformation()
        {
            string osVersion = GetOSVersion();
            string processorInfo = GetProcessorInfo();
            string graphicsCardInfo = GetGraphicsCardInfo();
            string diskInfo = GetDiskInfo();

            // Atualiza os rótulos com as informações obtidas
            labelOSVersion.Text = $"{osVersion}";
            labelProcessor.Text = $"{processorInfo}";
            labelGraphicsCard.Text = $"{graphicsCardInfo}";
            labelDiskInfo.Text = $"{diskInfo}";
        }

        private string GetOSVersion()
        {
            var osQuery = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
            var osInfo = osQuery.Get().OfType<ManagementObject>().FirstOrDefault();
            return osInfo?["Caption"]?.ToString() ?? "N/A";
        }

        private string GetProcessorInfo()
        {
            var processorQuery = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            var processorInfo = processorQuery.Get().OfType<ManagementObject>().FirstOrDefault();
            return processorInfo?["Name"]?.ToString() ?? "N/A";
        }

        private string GetGraphicsCardInfo()
        {
            var graphicsQuery = new ManagementObjectSearcher("SELECT Caption FROM Win32_VideoController");
            var graphicsInfo = graphicsQuery.Get().OfType<ManagementObject>().FirstOrDefault();
            return graphicsInfo?["Caption"]?.ToString() ?? "N/A";
        }

        private string GetDiskInfo()
        {
            var diskQuery = new ManagementObjectSearcher("SELECT Model FROM Win32_DiskDrive");
            var diskInfo = diskQuery.Get().OfType<ManagementObject>().FirstOrDefault();
            return diskInfo?["Model"]?.ToString() ?? "N/A";
        }


        private async Task CheckInternetConnection()
        {
            isInternetAvailable = await CheckInternetConnectionAsync();
            UpdateInternetInfoUI();
        }

        private async Task<bool> CheckInternetConnectionAsync()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var result = await ping.SendPingAsync("www.google.com", 1000);
                    return result.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private void UpdateInternetInfoUI()
        {
            // Use o método Invoke para atualizar os controles de interface a partir da thread principal.
            if (labelInternetStatus.InvokeRequired)
            {
                labelInternetStatus.Invoke(new Action(UpdateInternetInfoUI));
            }
            else
            {
                labelInternetStatus.Text = isInternetAvailable ? "Connected" : "Disconnected";
                labelInternetStatus.ForeColor = isInternetAvailable ? Color.Green : Color.Red;
            }
        }
        private void UpdateNetworkInfo()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in networkInterfaces)
            {
                if (adapter.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProperties = adapter.GetIPProperties();
                    if (ipProperties.GatewayAddresses.Count > 0)
                    {
                        var ipAddress = ipProperties.UnicastAddresses
                            .FirstOrDefault(ip => !IPAddress.IsLoopback(ip.Address) && ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address;

                        var gateway = ipProperties.GatewayAddresses.FirstOrDefault()?.Address;
                        var subnetMask = ipProperties.UnicastAddresses
                            .FirstOrDefault(ip => !IPAddress.IsLoopback(ip.Address) && ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.IPv4Mask;

                        // Utilize o método Invoke para atualizar os controles da interface corretamente.
                        lblIPAddress.Invoke(new Action(() => lblIPAddress.Text = ipAddress?.ToString() ?? "N/A"));
                        lblGateway.Invoke(new Action(() => lblGateway.Text = gateway?.ToString() ?? "N/A"));
                        lblSubnetMask.Invoke(new Action(() => lblSubnetMask.Text = subnetMask?.ToString() ?? "N/A"));

                        lblIPAddress.Invoke(new Action(() => lblIPAddress.ForeColor = Color.White));
                        lblGateway.Invoke(new Action(() => lblGateway.ForeColor = Color.White));
                        lblSubnetMask.Invoke(new Action(() => lblSubnetMask.ForeColor = Color.White));

                        // Atualiza o nome da provedora da conexão ativa.
                        providerName = adapter.Description;

                        // Use o método Invoke para atualizar o controle lblProvider corretamente.
                        lblProvider.Invoke(new Action(() => lblProvider.Text = providerName));

                        return; // Mostrar informações apenas para o primeiro adaptador de rede ativo.
                    }
                }
            }

            // Caso não encontre nenhuma conexão ativa, defina as informações como N/A.
            lblIPAddress.Invoke(new Action(() => lblIPAddress.Text = "N/A"));
            lblGateway.Invoke(new Action(() => lblGateway.Text = "N/A"));
            lblSubnetMask.Invoke(new Action(() => lblSubnetMask.Text = "N/A"));
            lblProvider.Invoke(new Action(() => lblProvider.Text = "No Connection"));
        }

        private void UpdateGPUUsage()
        {
            try
            {
                float gpuUsage = gpuCounter.NextValue();
                labelGPU.Text = $"{gpuUsage:F1}%";
            }
            catch (InvalidOperationException)
            {
                // Ocorre quando a categoria de desempenho da GPU não é encontrada.
                labelGPU.Text = "N/A";
            }
        }
        private bool IsUsingDNSProxy()
        {
            try
            {
                var query = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE DNSProxyEnabled = true");
                var dnsProxy = query.Get();
                return dnsProxy.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private bool IsUsingVPN()
        {
            try
            {
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in networkInterfaces)
                {
                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ppp)
                    {
                        // Verifica se a conexão é uma VPN.
                        if ((adapter.OperationalStatus == OperationalStatus.Up) || (adapter.OperationalStatus == OperationalStatus.Unknown))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        
        //botões


        private void frmPrincipal_Load(object sender, EventArgs e)
        {
            GetSystemInformation();
        }

        private void frmInfo_Load(object sender, EventArgs e)
        {

        }
    }
}
