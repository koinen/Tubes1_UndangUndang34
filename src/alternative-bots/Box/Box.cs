
using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ------------------------------------------------------------------
// Box
// ------------------------------------------------------------------
// Bot Box bergerak ke salah satu sudut arena dan bergerak di sana.
// Bot ini akan bergerak ke sudut arena yang paling aman, dan bergerak dalam bentuk kotak.
// Bot ini akan menembakkan peluru ke musuh yang paling dekat.
// ------------------------------------------------------------------
public class Box : Bot
{
    const double deg_to_rad = Math.PI / 180.0;
    const double rad_to_deg = 180.0 / Math.PI;
    const int boxWidth = 250;
    const int boxHeight = 200;
    const int GUN_FACTOR = 500;
    int corner;
    Dictionary<int, int> enemyLocations = new Dictionary<int, int>();
    int closestBotId;
    double closestBotDistance;
    const int padding = 20;
    double currentHeading;
    double movement;
    int dir;
    int state, count;
    int cornerChange;
    Rectangle box;
    static void Main(string[] args)
    {
        new Box().Start();
    }
    Box() : base(BotInfo.FromFile("Box.json")) { }
    public override void Run()
    {
        state = 0;
        cornerChange = 0;
        count = 0;
        closestBotId = -1;
        closestBotDistance = -1;
        if (X < ArenaWidth / 2) // mengambil sudut arena yang paling dekat 
        {
            if (Y < ArenaHeight / 2) {
                box = new Rectangle(padding, padding, boxWidth, boxHeight);
                corner = 3;
                Console.WriteLine("Box.cs: Run(): Box in bottom left corner");
                dir = 1;
            } else {
                box = new Rectangle(padding, ArenaHeight - padding - boxHeight, boxWidth, boxHeight);
                corner = 2;
                dir = 1;
                Console.WriteLine("Box.cs: Run(): Box in top left corner");
            }
        } else {
            if (Y < ArenaHeight / 2) {
                box = new Rectangle(ArenaWidth - padding - boxWidth, padding, boxWidth, boxHeight);
                corner = 4;
                Console.WriteLine("Box.cs: Run(): Box in bottom right corner");
                dir = -1;
            } else {
                box = new Rectangle(ArenaWidth - padding - boxWidth, ArenaHeight - padding - boxHeight, boxWidth, boxHeight);
                corner = 1;
                Console.WriteLine("Box.cs: Run(): Box in top right corner");
                dir = -1;
            }
        }
        // Set colors
        BodyColor = Color.Red;
        TurretColor = Color.Black;
        RadarColor = Color.Yellow;
        BulletColor = Color.Green;
        ScanColor = Color.Green;

        // Move to a corner
        GoCorner();
        AdjustGunForBodyTurn = true;
        while (IsRunning)
        {
            SetTurnRadarLeft(Double.PositiveInfinity);
            AdjustMovement();
            if (DistanceRemaining == 0) {
                TurnLeft(CalcBearing(currentHeading + 90 * dir));
                Forward(movement);
            }
            if (TurnRemaining == 0) {
                currentHeading = (currentHeading + 90 * dir) % 360;
            }
            Go();
        }
    }

    public void AdjustMovement() {
        if (currentHeading == 90) { // facing right wall
            movement = boxWidth;
        }
        else if (currentHeading == 180) { // facing top wall
            movement = boxHeight;
        }
        else if (currentHeading == 270) { // facing left wall
            movement = boxWidth;
        }
        else { // facing bottom wall
            movement = boxHeight;
        }
    }
    private void GoCorner()
    {
        // go to chosen corner
        double angle, movement;
        if (corner == 1) {
            angle = Math.Atan2(ArenaHeight - padding - Y, ArenaWidth - padding - X) * rad_to_deg;
        }
        else if (corner == 2) {
            angle = 180 - Math.Atan2(ArenaHeight - Y - padding, X - padding) * rad_to_deg;
        }
        else if (corner == 3) {
            angle = 180 + Math.Atan2(Y - padding, X - padding) * rad_to_deg;
        }
        else {
            angle = 360 - Math.Atan2(Y - padding, ArenaWidth - padding - X) * rad_to_deg;
        }
        TurnLeft(CalcBearing(angle));
        Forward(1000);
    }

    // We saw another bot -> stop and fire!
    public override void OnScannedBot(ScannedBotEvent evt)
    {
        if (state == 0) { 
            if (300 < evt.X && evt.X < 500 && 200 < evt.Y && evt.Y < 400) { // membagi arena menjadi 5 bagian, membagi jumlah musuh ke dalam 5 bagian untuk menentukan sudut yang paling aman
                enemyLocations[evt.ScannedBotId] = 0;
            }
            else if (evt.X < ArenaWidth / 2) {
                if (evt.Y < ArenaHeight / 2) {
                    enemyLocations[evt.ScannedBotId] = 3;
                } else {
                    enemyLocations[evt.ScannedBotId] = 2;
                }
            } else {
                if (evt.Y < ArenaHeight / 2) {
                    enemyLocations[evt.ScannedBotId] = 4;
                } else {
                    enemyLocations[evt.ScannedBotId] = 1;
                }
            }
            if (closestBotDistance < 0 || DistanceTo(evt.X, evt.Y) < closestBotDistance) {
                closestBotDistance = DistanceTo(evt.X, evt.Y);
                closestBotId = evt.ScannedBotId;
            }
            count++;
            if (count >= EnemyCount) {
                state = 1; // Switch to shooting
                if (cornerChange <= 2) {
                    cornerChange++;
                    int corner1 = 0, corner2 = 0, corner3 = 0, corner4 = 0;
                    foreach (KeyValuePair<int, int> entry in enemyLocations) {
                        if (entry.Value == 1) {
                            corner1++;
                        } else if (entry.Value == 2) {
                            corner2++;
                        } else if (entry.Value == 3) {
                            corner3++;
                        } else {
                            corner4++;
                        }
                    }
                    int cornerTemp = corner;
                    if (corner1 <= corner2 && corner1 <= corner3 && corner1 <= corner4) {
                        corner = 1;
                    } else if (corner2 <= corner1 && corner2 <= corner3 && corner2 <= corner4) {
                        corner = 2;
                    } else if (corner3 <= corner1 && corner3 <= corner2 && corner3 <= corner4) {
                        corner = 3;
                    } else {
                        corner = 4;
                    }
                    Console.WriteLine("current corner: " + corner);
                    Console.WriteLine("Safest corner: " + corner);
                    if (cornerTemp != corner) {
                        Console.WriteLine("Box.cs: OnScannedBot(): Changing corner");
                        GoCorner();
                    }
                }
            }
        }
        if (evt.ScannedBotId == closestBotId) { // menembak musuh yang paling dekat
            SetTurnRadarRight(RadarTurnRemaining);
            double absBearing = BearingTo(evt.X, evt.Y) + Direction;
            SetTurnGunLeft(NormalizeRelativeAngle(GunBearingTo(evt.X, evt.Y) + Math.Max((1 - DistanceTo(evt.X, evt.Y) / (GUN_FACTOR+100)), 0) * Math.Asin(evt.Speed / 8) * Math.Sin(evt.Direction - absBearing) * rad_to_deg));	
            double dist = DistanceTo(evt.X, evt.Y);
            if (dist < 500 && dist > 0 || EnemyCount <= 3) { // setelah menembak musuh yang paling dekat, scan lagi
                SetFire(80/dist);
                state = 0;
                closestBotId = -1;
                closestBotDistance = -1;
            } else { // jika musuh terlalu jauh, scan musuh yang lain
                state = 0;
                closestBotId = -1;
                closestBotDistance = -1;
            }
        }
    }
    public override void OnBotDeath(BotDeathEvent evt)
    {
        if (enemyLocations.ContainsKey(evt.VictimId)) { // jika musuh mati, hapus dari list musuh
            enemyLocations.Remove(evt.VictimId);
        }
    }   
}
public class PointDouble
{
    public double X;
    public double Y;

    public PointDouble(double x, double y)
    {
        X = x;
        Y = y;
    }
    public PointDouble(PointDouble p)
    {
        X = p.X;
        Y = p.Y;
    }
    public double distance(PointDouble p)
    {
        return Math.Sqrt((X - p.X) * (X - p.X) + (Y - p.Y) * (Y - p.Y));
    }
}