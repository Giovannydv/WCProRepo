using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WCPro.Models;
using static WCPro.Models.ProductModels;

namespace WCPro.Pages
{
    public partial class settings_page : Page
    {
        // =========================================
        // GLOBAL VARIABLES
        // =========================================

        // private Ellipse selectedNode;

        //private Grid selectedNode;

        private Canvas selectedNode;

        private Point mouseOffset;

        private int nodeCounter = 1;

        private bool isDragging = false;

        private NodeModel selectedNodeModel;

        private List<NodeModel> nodes = new List<NodeModel>();

        private string currentImagePath = "";

        private ScaleTransform canvasScale =
            new ScaleTransform(1.0, 1.0);

        private Point panStartPoint;

        private bool isPanning = false;

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public settings_page()
        {
            InitializeComponent();

            EditorContainer.RenderTransform = canvasScale;
        }

        // =========================================
        // LOAD IMAGE
        // =========================================

        private void BtnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg";

            if (dialog.ShowDialog() == true)
            {
                currentImagePath = dialog.FileName;

                BitmapImage bitmap = new BitmapImage(new Uri(dialog.FileName));

                HarnessImage.Source = bitmap;

                HarnessImage.Width = bitmap.PixelWidth;

                HarnessImage.Height = bitmap.PixelHeight;

                NodeCanvas.Width = bitmap.PixelWidth;

                NodeCanvas.Height = bitmap.PixelHeight;

                EditorContainer.Width = bitmap.PixelWidth;

                EditorContainer.Height = bitmap.PixelHeight;

                EventLogList.Items.Add("[SYSTEM] Image loaded.");
            }
        }

        // =========================================
        // CREATE NODE
        // =========================================

        private void NodeCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Avoid creating node while dragging

            if (isDragging)
                return;

            Point position = e.GetPosition(NodeCanvas);

            CreateNode(position.X, position.Y);
        }

        private void NodeCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoom = e.Delta > 0 ? 0.1 : -0.1;

            if (canvasScale.ScaleX + zoom < 0.2)
                return;

            canvasScale.ScaleX += zoom;

            canvasScale.ScaleY += zoom;
        }

        // =========================================
        // EDITOR CONTROLS
        // =========================================

        private void Editor_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            isPanning = true;

            panStartPoint = e.GetPosition(this);

            Cursor = Cursors.SizeAll;
        }

        private void Editor_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            isPanning = false;

            Cursor = Cursors.Arrow;
        }

        private void Editor_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isPanning)
                return;

            Point currentPoint = e.GetPosition(this);

            double offsetX = currentPoint.X - panStartPoint.X;

            double offsetY = currentPoint.Y - panStartPoint.Y;

            panStartPoint = currentPoint;
        }

        // =========================================
        // EDITOR CONTROLS
        // =========================================

        // =========================================
        // CREATE NODE METHOD
        // =========================================

        private void CreateNode(double x, double y)
        {
            NodeModel model = new NodeModel
            {
                Id = nodeCounter,

                Name = $"NODE_{nodeCounter}",

                MacAddress = "",

                NodeType = "Clip",

                X = x,
                Y = y
            };

            Canvas nodeContainer = CreateVisualNode(model);

            Canvas.SetLeft(nodeContainer, model.X - 22);

            Canvas.SetTop(nodeContainer, model.Y - 10);

            NodeCanvas.Children.Add(nodeContainer);

            nodes.Add(model);

            AddNodeToSidebar(model);

            EventLogList.Items.Add($"[NODE] Created {model.Name}");

            nodeCounter++;
        }

        private void Node_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas node = sender as Canvas;

            ContextMenu menu = new ContextMenu();

            // =========================================
            // DELETE NODE
            // =========================================

            MenuItem deleteItem = new MenuItem
            {
                Header = "Delete Node"
            };

            deleteItem.Click += (s, ev) =>
            {
                DeleteNode(node);
            };

            // =========================================
            // GREEN
            // =========================================

            MenuItem greenItem = new MenuItem
            {
                Header = "Green"
            };

            greenItem.Click += (s, ev) =>
            {
                ChangeNodeColor(node, Brushes.LimeGreen);
            };

            // =========================================
            // RED
            // =========================================

            MenuItem redItem = new MenuItem
            {
                Header = "Red"
            };

            redItem.Click += (s, ev) =>
            {
                ChangeNodeColor(node, Brushes.Red);
            };

            // =========================================
            // YELLOW
            // =========================================

            MenuItem yellowItem = new MenuItem
            {
                Header = "Yellow"
            };

            yellowItem.Click += (s, ev) =>
            {
                ChangeNodeColor(node, Brushes.Gold);
            };

            // =========================================
            // BLUE
            // =========================================

            MenuItem blueItem = new MenuItem
            {
                Header = "Blue"
            };

            blueItem.Click += (s, ev) =>
            {
                ChangeNodeColor(node, Brushes.DeepSkyBlue);
            };

            // =========================================
            // ADD ITEMS
            // =========================================

            menu.Items.Add(deleteItem);

            menu.Items.Add(new Separator());

            menu.Items.Add(greenItem);

            menu.Items.Add(redItem);

            menu.Items.Add(yellowItem);

            menu.Items.Add(blueItem);

            node.ContextMenu = menu;
        }

        private Canvas CreateVisualNode(NodeModel model)
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

            Ellipse node = new Ellipse
            {
                Width = 35,
                Height = 35,

                Fill = Brushes.LimeGreen,

                Stroke = Brushes.White,

                StrokeThickness = 2,

                Cursor = Cursors.Hand
            };

            Canvas.SetLeft(node, 22);

            Canvas.SetTop(node, 10);

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

            // =========================================
            // EVENTS
            // =========================================

            container.MouseLeftButtonDown += Node_MouseLeftButtonDown;

            container.MouseMove += Node_MouseMove;

            container.MouseLeftButtonUp += Node_MouseLeftButtonUp;

            container.MouseRightButtonDown += Node_MouseRightButtonDown;

            // =========================================
            // ADD
            // =========================================

            container.Children.Add(node);

            container.Children.Add(label);

            return container;
        }

        private void DeleteNode(Canvas node)
        {
            NodeModel model = node.Tag as NodeModel;

            nodes.Remove(model);

            NodeCanvas.Children.Remove(node);

            RefreshNodeSidebar();

            EventLogList.Items.Add(
                $"[NODE] Deleted {model.Name}");
        }

        private void RefreshNodeSidebar()
        {
            NodeListPanel.Children.Clear();

            foreach (var node in nodes)
            {
                AddNodeToSidebar(node);
            }
        }

        private void ChangeNodeColor(Canvas node, Brush color)
        {
            Ellipse ellipse =
                node.Children[0] as Ellipse;

            ellipse.Fill = color;
        }

        // =========================================
        // ADD NODE TO SIDEBAR
        // =========================================

        private void AddNodeToSidebar(NodeModel model)
        {
            Border border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(17, 24, 39)),

                CornerRadius = new CornerRadius(10),

                Margin = new Thickness(0, 0, 0, 10),

                Padding = new Thickness(10)
            };

            StackPanel panel = new StackPanel();

            TextBlock title = new TextBlock
            {
                Text = model.Name,

                Foreground = Brushes.White,

                FontWeight = FontWeights.Bold,

                FontSize = 16
            };

            TextBlock type = new TextBlock
            {
                Text = model.NodeType,

                Foreground = Brushes.LightGray,

                FontSize = 12
            };

            panel.Children.Add(title);

            panel.Children.Add(type);

            border.Child = panel;

            NodeListPanel.Children.Add(border);
        }

        // =========================================
        // SELECT NODE
        // =========================================

        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // =========================================
            // REMOVE PREVIOUS GLOW
            // =========================================

            if (selectedNode != null)
            {
                Ellipse previousEllipse =
                    selectedNode.Children[0] as Ellipse;

                previousEllipse.Stroke = Brushes.White;

                previousEllipse.StrokeThickness = 2;
            }

            // =========================================
            // SELECT NEW NODE
            // =========================================

            selectedNode = sender as Canvas;

            selectedNodeModel = selectedNode.Tag as NodeModel;

            // =========================================
            // APPLY NEW GLOW
            // =========================================

            Ellipse ellipse =
                selectedNode.Children[0] as Ellipse;

            ellipse.Stroke = Brushes.DeepSkyBlue;

            ellipse.StrokeThickness = 4;

            // =========================================
            // LOAD UI DATA
            // =========================================

            TxtNodeName.Text = selectedNodeModel.Name;

            TxtMacAddress.Text = selectedNodeModel.MacAddress;

            // =========================================
            // SELECT NODE TYPE
            // =========================================

            foreach (ComboBoxItem item in CmbNodeType.Items)
            {
                if (item.Content.ToString() ==
                    selectedNodeModel.NodeType)
                {
                    CmbNodeType.SelectedItem = item;
                    break;
                }
            }

            // =========================================
            // DRAG SYSTEM
            // =========================================

            mouseOffset = e.GetPosition(NodeCanvas);

            selectedNode.CaptureMouse();

            isDragging = true;

            e.Handled = true;
        }

        private void Node_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectedNode != null &&
                e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(NodeCanvas);

                // =========================================
                // SAVE TRUE CENTER POSITION
                // =========================================

                if (selectedNodeModel != null)
                {
                    selectedNodeModel.X = position.X;

                    selectedNodeModel.Y = position.Y;
                }

                // =========================================
                // DRAW USING INTERNAL OFFSETS
                // =========================================

                Canvas.SetLeft(selectedNode, position.X - 22);

                Canvas.SetTop(selectedNode, position.Y - 10);
            }
        }

        // =========================================
        // RELEASE NODE
        // =========================================

        private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedNode != null)
            {
                selectedNode.ReleaseMouseCapture();

                selectedNode = null;

                isDragging = false;
            }
        }

        // =========================================
        // APPLY NODE CHANGES
        // =========================================

        private void BtnApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            if (selectedNodeModel == null)
                return;

            // =========================================
            // UPDATE MODEL
            // =========================================

            selectedNodeModel.Name = TxtNodeName.Text;

            selectedNodeModel.MacAddress = TxtMacAddress.Text;

            if (CmbNodeType.SelectedItem is ComboBoxItem item)
            {
                selectedNodeModel.NodeType = item.Content.ToString();
            }

            // =========================================
            // UPDATE VISUAL LABEL
            // =========================================

            if (selectedNode != null)
            {
                TextBlock label =
                    selectedNode.Children[1] as TextBlock;

                label.Text = selectedNodeModel.Name;
            }

            // =========================================
            // OPTIONAL COLOR BY TYPE
            // =========================================

            if (selectedNode != null)
            {
                Ellipse ellipse =
                    selectedNode.Children[0] as Ellipse;

                switch (selectedNodeModel.NodeType)
                {
                    case "Clip":
                        ellipse.Fill = Brushes.LimeGreen;
                        break;

                    case "LED":
                        ellipse.Fill = Brushes.Gold;
                        break;

                    case "Sensor":
                        ellipse.Fill = Brushes.DeepSkyBlue;
                        break;

                    case "Relay":
                        ellipse.Fill = Brushes.OrangeRed;
                        break;

                    default:
                        ellipse.Fill = Brushes.Gray;
                        break;
                }
            }

            // =========================================
            // EVENT LOG
            // =========================================

            EventLogList.Items.Add(
                $"[CONFIG] Updated {selectedNodeModel.Name}");
        }

        // =========================================
        // CLEAR ALL NODES
        // =========================================

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            NodeCanvas.Children.Clear();

            NodeListPanel.Children.Clear();

            nodes.Clear();

            selectedNode = null;

            selectedNodeModel = null;

            nodeCounter = 1;

            EventLogList.Items.Add("[SYSTEM] All nodes cleared.");
        }

        // =========================================
        // SAVE PRODUCT
        // =========================================

        private void BtnSaveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProductModel product = new ProductModel
                {
                    ProductName = "DefaultProduct",

                    ImagePath = currentImagePath,

                    Nodes = nodes
                };

                string json = JsonSerializer.Serialize(product,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                SaveFileDialog dialog = new SaveFileDialog();

                dialog.Filter = "WC Product|*.wcproduct";

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, json);

                    EventLogList.Items.Add("[SYSTEM] Product saved.");
                }
            }
            catch (Exception ex)
            {
                EventLogList.Items.Add($"[ERROR] {ex.Message}");
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow main = (MainWindow)Application.Current.MainWindow;
            main.MainFrame.Navigate(new LandingPage());
        }

        private void BtnLoadProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();

                dialog.Filter = "WC Product|*.wcproduct";

                if (dialog.ShowDialog() == true)
                {
                    string json = File.ReadAllText(dialog.FileName);

                    ProductModel product =
                        JsonSerializer.Deserialize<ProductModel>(json);

                    if (product == null)
                        return;

                    // Clear existing

                    NodeCanvas.Children.Clear();

                    NodeListPanel.Children.Clear();

                    nodes.Clear();

                    // Load image

                    currentImagePath = product.ImagePath;

                    BitmapImage bitmap =
                        new BitmapImage(new Uri(product.ImagePath));

                    HarnessImage.Source = bitmap;

                    NodeCanvas.Width = bitmap.PixelWidth;

                    NodeCanvas.Height = bitmap.PixelHeight;

                    // Restore nodes

                    foreach (var node in product.Nodes)
                    {
                        RestoreNode(node);
                    }

                    EventLogList.Items.Add("[SYSTEM] Product loaded.");
                }
            }
            catch (Exception ex)
            {
                EventLogList.Items.Add($"[ERROR] {ex.Message}");
            }
        }
        private void RestoreNode(NodeModel model)
        {
            Canvas nodeContainer = CreateVisualNode(model);

            Canvas.SetLeft(nodeContainer, model.X - 22);

            Canvas.SetTop(nodeContainer, model.Y - 10);

            NodeCanvas.Children.Add(nodeContainer);

            nodes.Add(model);

            AddNodeToSidebar(model);

            if (model.Id >= nodeCounter)
            {
                nodeCounter = model.Id + 1;
            }
        }
    }
}