using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ConquestBot, meminimalisasikan jarak menuju bot target dan menembakkan peluru sesuai dengan jarak tersebut

public class ConquestBot : Bot
{
    // ID musuh
    private int enemyID;
    private double enemyX, enemyY = 0;

    // Memulaikan bot
    static void Main(string[] args)
    {
        new ConquestBot().Start();
    }

    // Constructor, yang memuat file konfigurasi bot
    ConquestBot() : base(BotInfo.FromFile("Conquest.json")) { }

    public override void Run()
    {
        // Setiap bagian badan dari bot dipisahkan
        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;
        AdjustRadarForGunTurn = true;
        enemyID = -1;
        TargetSpeed = 8;

        while (IsRunning)
        {
            // Selama bot tidak ada target, maka radar akan berputar
            if (RadarTurnRemaining == 0.0)
            {
                SetTurnRadarLeft(double.PositiveInfinity);
            }
            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {

        // Jika bot tidak memiliki target atau target yang di lock adalah bot yang baru di scan
        if (enemyID == -1 || e.ScannedBotId == enemyID)
        {
            // Radar targeting, mengunci target
            double radarTurn = RadarBearingTo(e.X, e.Y);

            // Memastikan agar radar berputar mengitari target
            double extraTurn = Math.Min(Math.Atan(36 / Distance(X, Y, e.X, e.Y)) * (180 / Math.PI), 45);

            if (radarTurn < 0)
                radarTurn -= extraTurn;
            else
                radarTurn += extraTurn;

            SetTurnRadarLeft(radarTurn);


            // Penembakan
            enemyY = e.Y;
            enemyX = e.X;
            enemyID = e.ScannedBotId;

            double currentBulletPower = 0;

            double distance = Distance(X, Y, e.X, e.Y);


            // Mengatur kekuatan tembakan berdasarkan jarak
            if (distance > 300)
            {
                currentBulletPower = 0.5;
            }
            else if (distance >= 200)
            {
                currentBulletPower = 1;
            }
            else if (distance >= 100)
            {
                currentBulletPower = 2;
            }
            // Jike kecepatan bot target rendah atau jarak target kecil, maka kekuatan tembakan akan dimaksimalkan
            else if (distance < 100 || e.Speed < 1)
            {
                currentBulletPower = 3;
            }

            // Menghitung sudut dari bot ke target
            double bearingFromGun = GunBearingTo(e.X, e.Y);
            double bearingFromBody = BearingTo(e.X, e.Y);

            // Menghitung perbedaan sudut dari arah bot ke target
            double relative = e.Direction - Direction;

            // Menghitung koreksi terhadap sudut tembakan awal, mempertimbangkan kecepatan, jarak, dan arah target
            double correction;
            
            if(90 <= relative && relative <= 270)
            {
                correction = e.Speed+relative/distance;
            }
            else
            {
                correction = -relative/distance-e.Speed;
            }


            // Mengatur arah tembakan dan badan bot
            SetTurnGunLeft(bearingFromGun + correction);

            SetTurnLeft(bearingFromBody);

            // Menembakkan peluru
            SetFire(currentBulletPower);
        }
    }

    public override void OnBotDeath(BotDeathEvent e)
    {
        // Jika target mati, maka bot akan mencari target baru
        if (e.VictimId == enemyID)
        {
            enemyID = -1;
        }
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Jika bot menabrak dinding, maka bot akan berbalik arah dan mencari target baru, memastikan tidak mengejar target invalid
        enemyID = -1;
        TargetSpeed = -TargetSpeed;
    }

    // Mengembalikan jarak antara dua titik
    private double Distance(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
    }
}
