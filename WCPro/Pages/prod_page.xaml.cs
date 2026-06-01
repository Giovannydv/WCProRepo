using Microsoft.Win32;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static WCPro.Models.ProductModels;
using System.Windows.Media.Effects;
using WCPro.Models;
using WCPro.Services;

namespace WCPro.Pages
{
    /// <summary>
    /// Interaction logic for prod_page.xaml
    /// </summary>
    public partial class prod_page : Page
    {
        private List<Canvas> runtimeNodes =
            new List<Canvas>();

        private DispatcherTimer simulationTimer =
            new DispatcherTimer();

        private Random random =
            new Random();

        private List<ValidationSequenceStep> validationSequenceSteps =
            new List<ValidationSequenceStep>();

        private int currentStepIndex = 0;

        private string currentSequencePath = "";

        private bool sequenceLoaded = false;

        private TcpServerService tcpServer;


        public prod_page()
        {
            InitializeComponent();
            simulationTimer.Stop();

            tcpServer =
                new TcpServerService();

            tcpServer.MessageReceived +=
                OnTcpMessageReceived;

            tcpServer.Start();
        }


        private void OnTcpMessageReceived(
                string message)
        {
            Dispatcher.Invoke(() =>
            {
                AddRuntimeLog(
                    $"TCP: {message}");

                if (
                    int.TryParse(
                        message,
                        out int nodeId))
                {
                    ProcessValidation(
                        nodeId);
                }
            });
        }


        private void BtnLoadSequence_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog dialog =
                new OpenFileDialog();

            dialog.Filter =
                "Sequence Files|*.json";

            if (dialog.ShowDialog() == true)
            {
                currentSequencePath =
                    dialog.FileName;

                LoadValidationSequence(
                    currentSequencePath);

                TxtSequenceName.Text =
                    System.IO.Path.GetFileName(currentSequencePath);

                AddRuntimeLog(
                    validationSequenceSteps[0].Description);

                AddRuntimeLog(
                    $"SEQUENCE LOADED: {TxtSequenceName.Text}");
            }
        }

        private void LoadValidationSequence(string filePath)
        {

            validationSequenceSteps.Clear();
            
            try
            {
                string json =
                    File.ReadAllText(filePath);

                validationSequenceSteps =
                    JsonSerializer.Deserialize<
                        List<ValidationSequenceStep>>(
                            json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                sequenceLoaded = true;

                foreach (var step in validationSequenceSteps)
                {
                    AddRuntimeLog(
                        $"STEP={step.StepNumber} NODE={step.NodeId} DESC={step.Description}");

                    HighlightExpectedNode();
                }

                AddRuntimeLog(
                    "VALIDATION SEQUENCE LOADED");

                // =====================================
                // RESET STEP INDEX
                // =====================================

                currentStepIndex = 0;

                // =====================================
                // SHOW FIRST STEP
                // =====================================

                StatusText.Text = "SEQUENCE READY";

                StatusText.Foreground =
                    Brushes.DeepSkyBlue;



                if (validationSequenceSteps.Count > 0)
                {
                    TxtValidationStep.Text =
                        $"STEP 1: {validationSequenceSteps[0].Description}";
                }
            }
            catch (Exception ex)
            {
                AddRuntimeLog(
                    $"SEQUENCE LOAD ERROR: {ex.Message}");
            }
        }

        private void ProcessValidation(int activatedNodeId)
        {

            if (!sequenceLoaded)
            {
                AddRuntimeLog(
                    "NO VALIDATION SEQUENCE LOADED");

                return;
            }


            if (currentStepIndex >= validationSequenceSteps.Count)
                return;

            ValidationSequenceStep currentStep =
                validationSequenceSteps[currentStepIndex];

            // =========================================
            // CORRECT NODE
            // =========================================

            if (activatedNodeId == currentStep.NodeId)
            {
                AddRuntimeLog(
                    $"STEP {currentStep.StepNumber} OK");


                foreach (var node in runtimeNodes)
                {
                    NodeModel model =
                        node.Tag as NodeModel;

                    if (model.Id == activatedNodeId)
                    {
                        Ellipse ellipse =
                            node.Children[0] as Ellipse;

                        ellipse.Fill =
                            Brushes.LimeGreen;

                        ellipse.Stroke =
                            Brushes.LimeGreen;

                        ellipse.StrokeThickness = 4;

                        ellipse.Effect = null;

                        break;
                    }
                }


                TxtValidationStep.Text =
                    $"STEP {currentStep.StepNumber} COMPLETE";

                StatusText.Text =
                    "VALIDATION OK";

                StatusText.Foreground =
                    Brushes.LimeGreen;

                currentStepIndex++;

                HighlightExpectedNode();

                // =====================================
                // NEXT STEP
                // =====================================

                if (currentStepIndex < validationSequenceSteps.Count)
                {
                    ValidationSequenceStep nextStep =
                        validationSequenceSteps[currentStepIndex];

                    TxtValidationStep.Text =
                        $"STEP {nextStep.StepNumber}: {nextStep.Description}";
                }
                else
                {
                    TxtValidationStep.Text =
                        "VALIDATION COMPLETE";

                    StatusText.Text =
                        "PROCESS COMPLETE";

                    StatusText.Foreground =
                        Brushes.DeepSkyBlue;

                    ShowPassOverlay();
                }
            }

            // =========================================
            // WRONG NODE
            // =========================================

            else
            {
                AddRuntimeLog(
                    $"WRONG NODE: NODE_{activatedNodeId}");

                StatusText.Text =
                    "VALIDATION ERROR";

                StatusText.Foreground =
                    Brushes.Red;

                _ = HighlightWrongNode(
                    activatedNodeId);
            }
        }




        // =========================================
        // LOAD PRODUCT
        // =========================================

        private void BtnLoadProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();

                dialog.Filter = "WC Product|*.wcproduct";

                if (dialog.ShowDialog() == true)
                {
                    string json =
                        File.ReadAllText(dialog.FileName);

                    ProductModel product =
                        JsonSerializer.Deserialize<ProductModel>(json);

                    if (product == null)
                        return;

                    LoadRuntimeProduct(product);

                    StatusText.Text =
                        $"RUNNING: {product.ProductName}";

                    // simulationTimer.Start();
                }
            }
            catch
            {
                StatusText.Text = "ERROR LOADING PRODUCT";

                StatusText.Foreground = Brushes.Red;
            }
        }

        // =========================================
        // LOAD PRODUCT INTO RUNTIME
        // =========================================

        private void LoadRuntimeProduct(ProductModel product)
        {
            RuntimeCanvas.Children.Clear();

            runtimeNodes.Clear();

            // =========================
            // IMAGE
            // =========================

            BitmapImage bitmap =
                new BitmapImage(new System.Uri(product.ImagePath));

            HarnessImage.Source = bitmap;

            HarnessImage.Width = bitmap.PixelWidth;
            HarnessImage.Height = bitmap.PixelHeight;

            RuntimeCanvas.Width = bitmap.PixelWidth;
            RuntimeCanvas.Height = bitmap.PixelHeight;

            RuntimeContainer.Width = bitmap.PixelWidth;
            RuntimeContainer.Height = bitmap.PixelHeight;

            // =========================
            // NODES
            // =========================

            foreach (var node in product.Nodes)
            {
                CreateRuntimeNode(node);
            }

            currentStepIndex = 0;

        }

        // =========================================
        // CREATE RUNTIME NODE
        // =========================================

        private void CreateRuntimeNode(NodeModel model)
        {
            Canvas container = new Canvas
            {
                Width = 80,
                Height = 80,
                Tag = model
            };

            // =========================================
            // NODE
            // =========================================

            Ellipse ellipse = new Ellipse
            {
                Width = 35,
                Height = 35,

                Fill = Brushes.Gray,

                Stroke = Brushes.White,

                StrokeThickness = 2
            };

            Canvas.SetLeft(ellipse, 22);

            Canvas.SetTop(ellipse, 10);

            // =========================================
            // LABEL
            // =========================================

            TextBlock label = new TextBlock
            {
                Text = model.Name,

                Foreground = Brushes.Gold,

                Background = new SolidColorBrush(
                    Color.FromArgb(120, 0, 0, 0)),

                Padding = new Thickness(6, 2, 6, 2),

                FontSize = 12,

                FontWeight = FontWeights.Bold
            };

            Canvas.SetLeft(label, 8);

            Canvas.SetTop(label, 48);

            container.Children.Add(ellipse);

            container.Children.Add(label);

            Canvas.SetLeft(container, model.X - 22);

            Canvas.SetTop(container, model.Y - 10);

            RuntimeCanvas.Children.Add(container);

            runtimeNodes.Add(container);
        } 

        // =========================================
        // RUNTIME STATUS UPDATE
        // =========================================

        public void UpdateNodeStatus(int nodeId, bool active)
        {
            foreach (var node in runtimeNodes)
            {
                NodeModel model =
                    node.Tag as NodeModel;

                if (model.Id == nodeId)
                {
                    Ellipse ellipse =
                        node.Children[0] as Ellipse;

                    ellipse.Fill =
                        active
                        ? Brushes.LimeGreen
                        : Brushes.Red;

                    break;
                }
            }
        }











        // =========================================
        // BACK BUTTON
        // =========================================

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {

            simulationTimer.Stop();

            MainWindow main =
                (MainWindow)Application.Current.MainWindow;

            main.MainFrame.Navigate(
                new LandingPage());
        }

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (runtimeNodes.Count == 0)
                return;

            // =========================================
            // RANDOM NODE
            // =========================================

            int randomIndex =
                random.Next(runtimeNodes.Count);

            Canvas node =
                runtimeNodes[randomIndex];

            Ellipse ellipse =
                node.Children[0] as Ellipse;

            // =========================================
            // RANDOM STATE
            // =========================================

            int state = random.Next(5);

            NodeModel model =
                 node.Tag as NodeModel;

            ProcessValidation(model.Id);

            switch (state)

            {
                // OFFLINE

                case 0:

                    ellipse.Fill = Brushes.Gray;

                    StatusText.Text =
                        "NODE OFFLINE";

                    AddRuntimeLog($"{model.Name} OFFLINE");

                    StatusText.Foreground =
                        Brushes.Gray;

                    break;

                // ACTIVE

                case 1:

                    ellipse.Fill = Brushes.LimeGreen;

                    StatusText.Text =
                        "NODE ACTIVE";

                    AddRuntimeLog($"{model.Name} ACTIVE");

                    StatusText.Foreground =
                        Brushes.LimeGreen;

                    break;

                // ERROR

                case 2:

                    ellipse.Fill = Brushes.Red;

                    StatusText.Text =
                        "NODE ERROR";

                    AddRuntimeLog($"{model.Name} ERROR");

                    StatusText.Foreground =
                        Brushes.Red;

                    break;

                // WAITING

                case 3:

                    ellipse.Fill = Brushes.Gold;

                    StatusText.Text =
                        "WAITING SIGNAL";

                    AddRuntimeLog($"{model.Name} WAITING");

                    StatusText.Foreground =
                        Brushes.Gold;

                    break;

                // PROCESSING

                case 4:

                    ellipse.Fill = Brushes.DeepSkyBlue;

                    StatusText.Text =
                        "PROCESSING";

                    AddRuntimeLog($"{model.Name} PROCESSING");

                    StatusText.Foreground =
                        Brushes.DeepSkyBlue;

                    break;
            }
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            int active = 0;

            int errors = 0;

            foreach (var node in runtimeNodes)
            {
                Ellipse ellipse =
                    node.Children[0] as Ellipse;

                SolidColorBrush brush =
                    ellipse.Fill as SolidColorBrush;

                if (brush.Color == Colors.LimeGreen)
                {
                    active++;
                }

                if (brush.Color == Colors.Red)
                {
                    errors++;
                }
            }

            TxtActiveNodes.Text = active.ToString();

            TxtErrorNodes.Text = errors.ToString();
        }

        private void AddRuntimeLog(string message)
        {
            string timestamp =
                DateTime.Now.ToString("HH:mm:ss");

            RuntimeLog.Items.Insert(
                0,
                $"[{timestamp}] {message}");

            // Limit log size

            if (RuntimeLog.Items.Count > 100)
            {
                RuntimeLog.Items.RemoveAt(100);
            }
        }

        private async void ShowPassOverlay()
        {
            PassOverlay.Visibility = Visibility.Visible;

            AddRuntimeLog("PROCESS PASSED");

            // =========================================
            // WAIT
            // =========================================

            await Task.Delay(3000);

            PassOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnNode1_Click(object sender, RoutedEventArgs e)
        {
            ProcessValidation(1);
        }

        private void BtnNode2_Click(object sender, RoutedEventArgs e)
        {
            ProcessValidation(2);
        }

        private void BtnNode3_Click(object sender, RoutedEventArgs e)
        {
            ProcessValidation(3);
        }

        private void BtnNode4_Click(object sender, RoutedEventArgs e)
        {
            ProcessValidation(4);
        }

        private void HighlightExpectedNode()
        {
            foreach (var node in runtimeNodes)
            {
                Ellipse ellipse =
                    node.Children[0] as Ellipse;

                // NO tocar nodos ya validados

                if (ellipse.Fill == Brushes.LimeGreen)
                    continue;

                ellipse.Fill = Brushes.Gray;

                ellipse.Stroke = Brushes.White;

                ellipse.StrokeThickness = 2;

                ellipse.Effect = null;
            }

            if (currentStepIndex >= validationSequenceSteps.Count)
                return;

            int expectedNodeId =
                validationSequenceSteps[currentStepIndex].NodeId;

            foreach (var node in runtimeNodes)
            {
                NodeModel model =
                    node.Tag as NodeModel;

                if (model.Id == expectedNodeId)
                {
                    Ellipse ellipse =
                        node.Children[0] as Ellipse;

                    ellipse.Fill =
                        Brushes.DeepSkyBlue;

                    ellipse.Stroke =
                        Brushes.DeepSkyBlue;

                    ellipse.StrokeThickness = 5;

                    ellipse.Effect =
                        new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = Colors.DeepSkyBlue,

                            BlurRadius = 25,

                            ShadowDepth = 0,

                            Opacity = 1
                        };

                    break;
                }
            }
        }
        private async Task HighlightWrongNode(int nodeId)
        {
            foreach (var node in runtimeNodes)
            {
                NodeModel model =
                    node.Tag as NodeModel;

                if (model.Id == nodeId)
                {
                    Ellipse ellipse =
                        node.Children[0] as Ellipse;

                    Brush previousFill =
                        ellipse.Fill;

                    Brush previousStroke =
                        ellipse.Stroke;

                    ellipse.Fill = Brushes.Red;

                    ellipse.Stroke = Brushes.DarkRed;

                    ellipse.StrokeThickness = 4;

                    await Task.Delay(1200);

                    ellipse.Fill = previousFill;

                    ellipse.Stroke = previousStroke;

                    break;
                }
            }
        }
    }

}
