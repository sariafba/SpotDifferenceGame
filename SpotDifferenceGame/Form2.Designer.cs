using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using SpotDifferenceGame.Properties;


namespace SpotTheDifference
{
    // Add this before your Form2 class
    public class DifferenceCluster
    {
        public Point Center { get; set; }
        public int Count { get; set; }
    }

    public partial class SpotTheDifferenceForm : Form
    {
        private Bitmap image1, image2;
        private List<Point> differences = new List<Point>();
        private List<Point> foundDifferences = new List<Point>();
        private int maxAttempts = 10;
        private int attemptsLeft;
        private int timeLeft = 60; // 60 seconds by default
        private System.Windows.Forms.Timer gameTimer;
        private GameMode currentMode;
        private DifficultyLevel currentDifficulty;
        private SoundPlayer correctSound = new SoundPlayer(new System.IO.MemoryStream(Resources.correct));
        private SoundPlayer wrongSound = new SoundPlayer(new System.IO.MemoryStream(Resources.wrong));


        public SpotTheDifferenceForm()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Set up timer
            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 1000;
            gameTimer.Tick += GameTimer_Tick;

            // Set default mode and difficulty
            currentMode = GameMode.TimeLimit;
            currentDifficulty = DifficultyLevel.Easy;

            // Initialize UI
            UpdateAttemptsDisplay();
            UpdateTimerDisplay();
            UpdateFoundDifferencesDisplay();

            // Ensure picture boxes are initially disabled
            pictureBox1.Enabled = false;
            pictureBox2.Enabled = false;
        }

        private void ClearPictureBoxes()
        {
            pictureBox1.Image = image1; // Reset to original image
            pictureBox2.Image = image2;
        }

        private void btnLoadImages_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog.FileNames.Length == 2)
                {
                    try
                    {
                        image1 = new Bitmap(openFileDialog.FileNames[0]);
                        image2 = new Bitmap(openFileDialog.FileNames[1]);

                        // Resize images to fit in picture boxes
                        image1 = ResizeImage(image1, pictureBox1.Width, pictureBox1.Height);
                        image2 = ResizeImage(image2, pictureBox2.Width, pictureBox2.Height);

                        pictureBox1.Image = image1;
                        pictureBox2.Image = image2;

                        // Generate random differences based on difficulty
                        GenerateDifferences();

                        // Enable game controls
                        btnStartGame.Enabled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading images: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please select exactly two images.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(image, 0, 0, width, height);
            }
            return result;
        }

        private void GenerateDifferences()
        {
            if (image1 == null || image2 == null) return;
            differences = FindActualDifferences(image1, image2); // New: Real differences
            foundDifferences.Clear();
        }

        private List<Point> FindActualDifferences(Bitmap img1, Bitmap img2)
        {
            List<Point> allDiffs = new List<Point>();
            int tolerance = 50;
            int scanStep = 5; // Check every 5th pixel for performance

            // Step 1: Find all differing pixels
            for (int x = 0; x < img1.Width; x += scanStep)
            {
                for (int y = 0; y < img1.Height; y += scanStep)
                {
                    if (ColorDiff(img1.GetPixel(x, y), img2.GetPixel(x, y)) > tolerance)
                        allDiffs.Add(new Point(x, y));
                }
            }

            // Step 2: Cluster nearby points
            List<DifferenceCluster> clusters = new List<DifferenceCluster>();
            int clusterRadius = Math.Max(img1.Width, img1.Height) / 15;

            foreach (Point point in allDiffs)
            {
                bool addedToCluster = false;

                foreach (var cluster in clusters)
                {
                    if (Distance(point, cluster.Center) < clusterRadius)
                    {
                        // Update cluster center (average position)
                        cluster.Center = new Point(
                            (cluster.Center.X * cluster.Count + point.X) / (cluster.Count + 1),
                            (cluster.Center.Y * cluster.Count + point.Y) / (cluster.Count + 1)
                        );
                        cluster.Count++;
                        addedToCluster = true;
                        break;
                    }
                }

                if (!addedToCluster)
                    clusters.Add(new DifferenceCluster { Center = point, Count = 1 });
            }

            // Step 3: Return only cluster centers
            return clusters.Select(c => c.Center).ToList();
        }

        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
        private int ColorDiff(Color c1, Color c2)
        {
            return Math.Abs(c1.R - c2.R) +
                   Math.Abs(c1.G - c2.G) +
                   Math.Abs(c1.B - c2.B);
        }

        private List<Point> SimplifyDifferences(List<Point> rawDiffs)
        {
            List<Point> finalDiffs = new List<Point>();
            Random rand = new Random();

            // Keep only 5-10 differences
            int maxDiffs = 5 + (int)currentDifficulty; // Easy=5, Medium=7, Hard=10
            while (finalDiffs.Count < maxDiffs && rawDiffs.Count > 0)
            {
                int index = rand.Next(rawDiffs.Count);
                finalDiffs.Add(rawDiffs[index]);
                rawDiffs.RemoveAt(index);
            }
            return finalDiffs;
        }

        private void btnStartGame_Click(object sender, EventArgs e)
        {

            ClearPictureBoxes();


            // Reset game state
            foundDifferences.Clear();
            attemptsLeft = maxAttempts;
            timeLeft = 60; // Reset time

            // Update UI
            UpdateFoundDifferencesDisplay();
            UpdateAttemptsDisplay();
            UpdateTimerDisplay();

            // Start game based on mode
            if (currentMode == GameMode.TimeLimit)
            {
                gameTimer.Start();
            }

            // Enable picture boxes for clicking
            pictureBox1.Enabled = true;
            pictureBox2.Enabled = true;

            btnStartGame.Enabled = false;

            // Clear any previous drawings
            pictureBox1.Invalidate();
            pictureBox2.Invalidate();
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            HandleClick(e.Location, pictureBox1);
        }

        private void pictureBox2_MouseClick(object sender, MouseEventArgs e)
        {
            HandleClick(e.Location, pictureBox2);
        }

        private void HandleClick(Point clickPoint, PictureBox pictureBox)
        {
            if (differences.Count == 0) return;

            // Check if click is near any difference
            foreach (var diff in differences)
            {
                // Calculate distance between click and difference
                double distance = Math.Sqrt(Math.Pow(clickPoint.X - diff.X, 2) + Math.Pow(clickPoint.Y - diff.Y, 2));

                int clickRadius = Math.Max(image1.Width, image1.Height) / 20;

                if (distance < clickRadius) // Within 20 pixels is considered a hit
                {
                    if (!foundDifferences.Contains(diff))
                    {
                        // Correct difference found
                        foundDifferences.Add(diff);
                        correctSound.Play();

                        // Visual feedback
                        using (Graphics g = pictureBox.CreateGraphics())
                        {
                            g.DrawEllipse(new Pen(Color.Green, 3), diff.X - 15, diff.Y - 15, 30, 30);
                        }

                        UpdateFoundDifferencesDisplay();

                        // Check if all differences found
                        if (foundDifferences.Count == differences.Count)
                        {
                            EndGame(true);
                        }
                    }
                    return;
                }
            }

            // If we get here, it's a wrong click
            wrongSound.Play();

            // Visual feedback for wrong click
            using (Graphics g = pictureBox.CreateGraphics())
            {
                g.DrawEllipse(new Pen(Color.Red, 3), clickPoint.X - 15, clickPoint.Y - 15, 30, 30);
            }

            // Handle attempts in attempts-limited mode
            if (currentMode == GameMode.AttemptLimit)
            {
                attemptsLeft--;
                UpdateAttemptsDisplay();

                if (attemptsLeft <= 0)
                {
                    EndGame(false);
                }
            }
        }

        private void UpdateFoundDifferencesDisplay()
        {
            lblFound.Text = $"Found: {foundDifferences.Count} / {differences.Count}";
        }

        private void UpdateAttemptsDisplay()
        {
            lblAttempts.Text = $"Attempts left: {attemptsLeft}";
        }

        private void UpdateTimerDisplay()
        {
            lblTime.Text = $"Time left: {timeLeft}s";
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            timeLeft--;
            UpdateTimerDisplay();

            if (timeLeft <= 0)
            {
                EndGame(false);
            }
        }

        private void EndGame(bool won)
        {

            ClearPictureBoxes();

            gameTimer.Stop();
            pictureBox1.Enabled = false;
            pictureBox2.Enabled = false;

            if (won)
            {
                MessageBox.Show("Congratulations! You found all the differences!", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Game over! Try again.", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Re-enable the start button
            btnStartGame.Enabled = true;

            // Clear any drawings on the picture boxes
            pictureBox1.Invalidate();
            pictureBox2.Invalidate();
        }

        private void timeModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentMode = GameMode.TimeLimit;
            timeModeToolStripMenuItem.Checked = true;
            attemptsModeToolStripMenuItem.Checked = false;
        }

        private void attemptsModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentMode = GameMode.AttemptLimit;
            timeModeToolStripMenuItem.Checked = false;
            attemptsModeToolStripMenuItem.Checked = true;
        }

        private void easyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentDifficulty = DifficultyLevel.Easy;
            UpdateDifficultyMenu();
        }

        private void mediumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentDifficulty = DifficultyLevel.Medium;
            UpdateDifficultyMenu();
        }

        private void hardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentDifficulty = DifficultyLevel.Hard;
            UpdateDifficultyMenu();
        }

        private void UpdateDifficultyMenu()
        {
            easyToolStripMenuItem.Checked = (currentDifficulty == DifficultyLevel.Easy);
            mediumToolStripMenuItem.Checked = (currentDifficulty == DifficultyLevel.Medium);
            hardToolStripMenuItem.Checked = (currentDifficulty == DifficultyLevel.Hard);
        }
    }

    public enum GameMode
    {
        TimeLimit,
        AttemptLimit
    }

    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }
}