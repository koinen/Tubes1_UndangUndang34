using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class ConquestBot : Bot
{
    private int enemyID;
    private double enemyX, enemyY = 0;

    static void Main(string[] args)
    {
        new ConquestBot().Start();
    }

    ConquestBot() : base(BotInfo.FromFile("Conquest.json")) { }

    public override void Run()
    {
        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;
        AdjustRadarForGunTurn = true;
        enemyID = -1;
        TargetSpeed = 8;

        while (IsRunning)
        {
            if(RadarTurnRemaining == 0.0)
            {
                SetTurnRadarLeft(double.PositiveInfinity);
            }
            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {


        if (enemyID == -1 || e.ScannedBotId == enemyID)
        {
            double radarTurn = RadarBearingTo(e.X, e.Y);

            double extraTurn = Math.Min(Math.Atan(36 / Distance(X, Y, e.X, e.Y)) * (180 / Math.PI), 45);

            if (radarTurn < 0)
                radarTurn -= extraTurn;
            else
                radarTurn += extraTurn;

            SetTurnRadarLeft(radarTurn);

            enemyY = e.Y;
            enemyX = e.X;
            enemyID = e.ScannedBotId;

            double currentBulletPower = 0;

            double distance = Distance(X, Y, e.X, e.Y);
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
            else if (distance < 100 || e.Speed < 1)
            {
                currentBulletPower = 3;
            }

            double bearingFromGun = GunBearingTo(e.X, e.Y);
            double bearingFromBody = BearingTo(e.X, e.Y);
            double relative = e.Direction - Direction;
            double correction;
            
            if(90 <= relative && relative <= 270)
            {
                correction = e.Speed+relative/distance;
            }
            else
            {
                correction = -relative/distance-e.Speed;
            }

            SetTurnGunLeft(bearingFromGun + correction);

            SetTurnLeft(bearingFromBody);

            SetFire(currentBulletPower);
        }
    }

    public override void OnBotDeath(BotDeathEvent e)
    {
        if(e.VictimId == enemyID)
        {
            enemyID = -1;
        }
    }

    public override void OnHitWall(HitWallEvent e)
    {
        enemyID = -1;
        TargetSpeed = -TargetSpeed;
    }

    private double Distance(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
    }
}

