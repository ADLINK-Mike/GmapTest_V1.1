using GMap.NET.MapProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET;
using System.Drawing.Drawing2D;


namespace GmapTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
//            Assembly myAssembly = Assembly.GetExecutingAssembly();
//            Stream myStream = myAssembly.GetManifestResourceStream("MyNamespace.SubFolder.MyImage.bmp");
//            Bitmap bmp = new Bitmap(myStream);
        }
        // NOW TRAP THE FORM CLOSING EVENT

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // assume that X has been clicked and act accordingly.
            GlobalData.WorkerThreadIsRunning = false;
            System.Threading.Thread.Sleep(1000);
            Application.Exit();
        }


        // The original image.
        private Bitmap OriginalBitmap;

        // The rotated image.
        private Bitmap RotatedBitmap;
//        private String DDSPartitionName;

        private void btn_LoadMap_Click(object sender, EventArgs e)
        {
            // first check if worker thread is running. If not start it...
            //
            if (GlobalData.WorkerThreadIsRunning != true)
            {
                //Thread thr_DDS_Worker = new Thread(new ThreadStart(Program.OpenSpliceWorker));
                System.Threading.Thread thr_DDS_Worker = new System.Threading.Thread(() => Program.OpenSpliceWorker(Form1.ActiveForm));
                thr_DDS_Worker.Start();
                GlobalData.WorkerThreadIsRunning = true; // will set this to false to shut down later...
            }

            try
            {
                // get the bitmap images
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                // check that the listbox has been filed in...

                Stream myStream = myAssembly.GetManifestResourceStream("GmapTest.Properties.aircraft_n.png");
                Bitmap bmp = new Bitmap(myStream);
                bmp.MakeTransparent();

                map.DragButton = MouseButtons.Left;

//                map.MapProvider = GMapProviders.GoogleSatelliteMap;

//                map.MapProvider = GMapProviders.BingHybridMap;

                map.MapProvider = GMapProviders.GoogleMap;
                double lat = Convert.ToDouble(txt_Latitude.Text);
                double lon = Convert.ToDouble(txt_Longitude.Text);
                map.Position = new GMap.NET.PointLatLng(lat, lon);
                map.MinZoom = 1;
                map.MaxZoom = 100;
                map.Zoom = 10;


                GMapOverlay markers = new GMapOverlay("Aircraft positions");
                //            markers.Markers.Add(marker1);
                // iterate through the listbox and get lat / lon, alt.
                foreach (string s in lst_Aircraft.Items)
                {
                    //do stuff with (s);
                    // split to get values.
                    string[] words = s.Split(',');
                    PointLatLng p1 = new PointLatLng(Convert.ToDouble(words[4]), Convert.ToDouble(words[3]));
                    // NOW ROTATE THE MAP TO PROPER TRUE_TRACK (Direction of travel)
                    // Get the angle.
                    float angle = float.Parse(words[0]);
                    // Rotate.
                    RotatedBitmap = RotateBitmap(bmp, angle);
                    Console.WriteLine("Angle " + words[0] + "Lat " + words[4] + " Lon " + words[3]);
                    GMapMarker marker = new GMarkerGoogle(p1, /*bmp*/ RotatedBitmap);
                    marker.ToolTipText = words[1] + "-" + words[2] + "\n" + "Alt-" + words[5] + "m";
                    markers.Markers.Add(marker);
                }
                // now place a marker
                map.Overlays.Add(markers);

                System.Threading.Thread.Sleep(1000);
                tmr_timer1.Interval = 2000;
                tmr_timer1.Start();

            }
            catch
            {
                Console.WriteLine("Trapped an error but going to continue");
            }

        }

        private void txt_Longitude_TextChanged(object sender, EventArgs e)
        {

        }

        private void lbl_Longitude_Click(object sender, EventArgs e)
        {

        }

        private void txt_Latitude_TextChanged(object sender, EventArgs e)
        {

        }

        private void lbl_Latitude_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
        }
        // catch form close
        

        private void tmr_timer1_Tick(object sender, EventArgs e)
        {
            try { 
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                // load the file "C:\temp\aircraft.txt" into listbox
                //OpenFileDialog f = new OpenFileDialog();
                //if (f.ShowDialog() == DialogResult.OK)
                Stream myStream2 = myAssembly.GetManifestResourceStream("GmapTest.Properties.aircraft_n.png");
                Bitmap bmp = new Bitmap(myStream2);
                bmp.MakeTransparent();

                {
                    lst_Aircraft.Items.Clear();
                    //List<string> lines = new List<string>();
                    // now do it the new DDS Way!!!

                    //GlobalData.DDS_Lines = ADSBLinesmsg.message;
                    if (GlobalData.DDS_Data_Received == true)
                    {// get the DDS data lines and split into an array
                        string[] lines = GlobalData.DDS_Lines.Split('|');

//                        string[] lines = GlobalData.DDS_Lines.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                        for(int x=0;x< (lines.Length-1); x++)
                        {
                            lst_Aircraft.Items.Add(lines[x]);
                        }

                    }


/*                    using (StreamReader r = new StreamReader("/temp/aircraft.txt") )
                    {
                        string line;
                        while ((line = r.ReadLine()) != null)
                        {
                            lst_Aircraft.Items.Add(line);

                        }
                    }
*/                }
                // now call the update locations
                // for now, delete the overlay and re-add it, I know it is a cludge...
                map.Overlays.Clear(); // remove the overlay
                GMapOverlay markers = new GMapOverlay("Aircraft positions");
                map.Overlays.Add(markers);
                //            markers.Markers.Add(marker1);
                // iterate through the listbox and get lat / lon, alt.
                foreach (string s in lst_Aircraft.Items)
                {
                    //do stuff with (s);
                    // split to get values.
                    string[] words = s.Split(',');
                    PointLatLng p1 = new PointLatLng(Convert.ToDouble(words[4]), Convert.ToDouble(words[3]));
                    Console.WriteLine("Lat" + words[4] + " Lon " + words[3]);
                    // NOW ROTATE THE MAP TO PROPER TRUE_TRACK (Direction of travel)
                    // Get the angle.
                    float angle = float.Parse(words[0]);
                    // Rotate.
                    RotatedBitmap = RotateBitmap(bmp, angle);
                    Console.WriteLine("Angle " + words[0] + "Lat " + words[4] + " Lon " + words[3]);
                    GMapMarker marker = new GMarkerGoogle(p1, /*bmp*/ RotatedBitmap);

                    //                GMapMarker marker = new GMarkerGoogle(p1, bmp);
                    // convert meters to feet
                    //                    int x = Int32.Parse(words[5]);
                    //                    float f = float.Parse(words[5]);

                    if (float.TryParse(words[5], out float f))
                    {
                        f = f * 3.28084F; // now in feet.
                        int x = (int)f;
                        marker.ToolTipText = words[1] + "-" + words[2] + "\n" + "Alt-" + /*words[5]*/ x.ToString() + "ft";
                    }  else
                    {
                        marker.ToolTipText = words[1] + "-" + words[2] + "\n" + "Alt-" + words[5] + "m";

                    }

                    markers.Markers.Add(marker);
                }
            } catch
            {
                Console.WriteLine("Timer Tick got an error but going to continue");
            }
        }

        private void map_Load(object sender, EventArgs e)
        {

        }

        private void map_DragDrop(object sender, DragEventArgs e)
        {
            Console.WriteLine("Map Drag - ");

        }

        private void map_MouseUp(object sender, MouseEventArgs e)
        {
            var center = map.Position;
            var upleft = map.ViewArea.LocationTopLeft; // this allows setting bounds for screen area top left
            var btmright = map.ViewArea.LocationRightBottom; // allows setting bounds for screen area bottom right

            string text = btmright.Lat.ToString() + "," + upleft.Lng.ToString() + "," + upleft.Lat.ToString() + "," + btmright.Lng.ToString();
//            System.IO.File.WriteAllText(@"C:\temp\region.txt", text);
            Console.WriteLine("Lat " + Convert.ToString(center.Lat) + " Lon " + Convert.ToString(center.Lng));
            txt_Latitude.Text = Convert.ToString(center.Lat);
            txt_Longitude.Text = Convert.ToString(center.Lng);

            ULLatTxt.Text = upleft.Lat.ToString();
            ULLngTxt.Text = upleft.Lng.ToString();
            LRLatTxt.Text = btmright.Lat.ToString();
            LRLngTxt.Text = btmright.Lng.ToString();

            // now trigger the DDS Send.
            GlobalData.upleft_lat = upleft.Lat;
            GlobalData.upleft_lng = upleft.Lng;
            GlobalData.btmright_lat = btmright.Lat;
            GlobalData.btmright_lng = btmright.Lng;
            GlobalData.ischanged = true;
        }


        // Return a bitmap rotated around its center.
        private Bitmap RotateBitmap(Bitmap bm, float angle)
        {
            try { 
                // Make a Matrix to represent rotation by this angle.
                Matrix rotate_at_origin = new Matrix();
                rotate_at_origin.Rotate(angle);

                // Rotate the image's corners to see how big
                // it will be after rotation.
                PointF[] points =
                {
                    new PointF(0, 0),
                    new PointF(bm.Width, 0),
                    new PointF(bm.Width, bm.Height),
                    new PointF(0, bm.Height),
                };
                rotate_at_origin.TransformPoints(points);
                float xmin, xmax, ymin, ymax;
                GetPointBounds(points, out xmin, out xmax, out ymin, out ymax);

                // Make a bitmap to hold the rotated result.
                int wid = (int)Math.Round(xmax - xmin);
                int hgt = (int)Math.Round(ymax - ymin);
                Bitmap result = new Bitmap(wid, hgt);

                // Create the real rotation transformation.
                Matrix rotate_at_center = new Matrix();
                rotate_at_center.RotateAt(angle,
                    new PointF(wid / 2f, hgt / 2f));

                // Draw the image onto the new bitmap rotated.
                using (Graphics gr = Graphics.FromImage(result))
                {
                    // Use smooth image interpolation.
                    gr.InterpolationMode = InterpolationMode.High;

                    // Clear with the color in the image's upper left corner.
                    gr.Clear(bm.GetPixel(0, 0));

                    //// For debugging. (Makes it easier to see the background.)
                    //gr.Clear(Color.LightBlue);

                    // Set up the transformation to rotate.
                    gr.Transform = rotate_at_center;

                    // Draw the image centered on the bitmap.
                    int x = (wid - bm.Width) / 2;
                    int y = (hgt - bm.Height) / 2;
                    gr.DrawImage(bm, x, y);
                }

                // Return the result bitmap.
                return result;
            } catch
            {
                return bm; // if an error return original bitmap
            }
        }

        // Find the bounding rectangle for an array of points.
        private void GetPointBounds(PointF[] points, out float xmin, out float xmax, out float ymin, out float ymax)
        {
            xmin = points[0].X;
            xmax = xmin;
            ymin = points[0].Y;
            ymax = ymin;
            foreach (PointF point in points)
            {
                if (xmin > point.X) xmin = point.X;
                if (xmax < point.X) xmax = point.X;
                if (ymin > point.Y) ymin = point.Y;
                if (ymax < point.Y) ymax = point.Y;
            }
        }

        // Make sure the form is big enough to show the rotated image.
        private void SizeForm()
        {
//            int wid = picRotated.Right + picRotated.Left;
//            int hgt = picRotated.Bottom + picRotated.Left;
//            this.ClientSize = new Size(
//                Math.Max(wid, this.ClientSize.Width),
//                Math.Max(hgt, this.ClientSize.Height));
        }

        private void Map_OnMapZoomChanged()
        {
            var center = map.Position;
            var upleft = map.ViewArea.LocationTopLeft; // this allows setting bounds for screen area top left
            var btmright = map.ViewArea.LocationRightBottom; // allows setting bounds for screen area bottom right

            string text = btmright.Lat.ToString() + "," + upleft.Lng.ToString() + "," + upleft.Lat.ToString() + "," + btmright.Lng.ToString();
            System.IO.File.WriteAllText(@"C:\temp\region.txt", text);
            Console.WriteLine("Lat " + Convert.ToString(center.Lat) + " Lon " + Convert.ToString(center.Lng));
            txt_Latitude.Text = Convert.ToString(center.Lat);
            txt_Longitude.Text = Convert.ToString(center.Lng);
            // now trigger the DDS Send.
            GlobalData.upleft_lat = upleft.Lat;
            GlobalData.upleft_lng = upleft.Lng;
            GlobalData.btmright_lat = btmright.Lat;
            GlobalData.btmright_lng = btmright.Lng;
            GlobalData.ischanged = true;

        }

        private void txtPartitionName_TextChanged(object sender, EventArgs e)
        {
//            DDSPartitionName = txtPartitionName.Text;
            GlobalData.DDSPartitionName = txtPartitionName.Text;
            Console.WriteLine("PartitionName Changed event -" + GlobalData.DDSPartitionName);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // assume that X has been clicked and act accordingly.
            GlobalData.WorkerThreadIsRunning = false;
            System.Threading.Thread.Sleep(1000);
            Application.Exit();
        }

        private void txtPartitionName_Leave(object sender, EventArgs e)
        {
            //            DDSPartitionName = txtPartitionName.Text;
            GlobalData.DDSPartitionName = txtPartitionName.Text;
            Console.WriteLine("PartitionName Leave event -" + GlobalData.DDSPartitionName);

        }
    }
}
