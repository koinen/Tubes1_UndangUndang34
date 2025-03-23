using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using System.Collections.Generic;
using System;

public class Perpendicular : Bot
{
    // Bot states
    int state, count, dir = 1;
    int closestBotId, lastBotId;
    double closestBotDistance;
    static void Main(string[] args) { new Perpendicular().Start(); }
    
    Perpendicular() : base(BotInfo.FromFile("Perpendicular.json")) { }

    public override void Run()
    {   
        Console.WriteLine("Perpendicular.cs: Run()");
        state = 0; // Scanning state
        closestBotDistance = -1;
        count = 0;
        closestBotId = -1;
        lastBotId = -1;
        Console.WriteLine("Variables reset");
        // Set bot colors
        BodyColor = Color.FromArgb(0x00, 0x99, 0x00);
        TurretColor = Color.FromArgb(0xFF, 0xA5, 0x00); // Orange
        RadarColor = Color.FromArgb(0xFF, 0xD7, 0x00);  // Gold
        BulletColor = Color.FromArgb(0xFF, 0x45, 0x00); // Orange-Red
        ScanColor = Color.FromArgb(0xFF, 0xFF, 0x00);   // Bright Yellow 
        TracksColor = Color.FromArgb(0x99, 0x33, 0x00); // Dark Orange
        GunColor = Color.FromArgb(0xCC, 0x55, 0x00);    // Medium Orange

        while (IsRunning)
        {
            if (RadarTurnRemaining == 0.0){
                SetTurnRadarRight(Double.PositiveInfinity);
            }
            Go();
        }
    }
    // Chase the enemy while keeping radar lock
    private void ChaseEnemy(ScannedBotEvent e)
    {
        // Compute radar turn required to face enemy, normalized
        double radarTurn = RadarBearingTo(e.X, e.Y);
        
        // Distance we want to scan from middle of enemy to either side (36 units)
        double extraTurn = Math.Min(Math.Atan(36 / DistanceTo(e.X, e.Y)) * (180 / Math.PI), 45);
        
        // Adjust the radar turn to overshoot and prevent slipping
        if (radarTurn < 0)
            radarTurn -= extraTurn;
        else
            radarTurn += extraTurn;

        // Turn the radar
        SetTurnRadarLeft(radarTurn);

        // Compute gun turn required to face enemy
        double gunTurn = GunBearingTo(e.X, e.Y);
        double turn = BearingTo(e.X, e.Y);
        double enemyDistance = DistanceTo(e.X, e.Y);
        double enemyLateralSpeed = e.Speed * Math.Sin((e.Direction - (turn + Direction) * Math.PI/180));
        double overcompensate = Speed == 0 ? 0 : enemyLateralSpeed/Speed;
        // Adjust TargetSpeed based on distance
        if (enemyDistance > 200) {
            SetTurnLeft(turn + overcompensate + Math.Max((90 - (Math.Abs(enemyDistance - 100) * 2)), 0)); // ensures smooth turn when distance reaches 100
            if (enemyDistance > 200) {
                dir = 1;
            }
            SetTurnGunLeft(gunTurn + enemyLateralSpeed);
            TargetSpeed = 8 * dir; 
            SetFire(1);
        } else if (enemyDistance >= 100) {
            double perpendicular = NormalizeRelativeAngle(BearingTo(e.X, e.Y) + 90);
            SetTurnLeft(perpendicular + overcompensate);
            TargetSpeed = 8 * dir;
            SetTurnGunLeft(gunTurn + enemyLateralSpeed*2);
            SetFire(2);
        } else if (enemyDistance < 50) {
            SetTurnLeft(turn);
            SetTurnGunLeft(gunTurn);
            TargetSpeed = -4; // Back off
            SetFire(3);
        }
        // Fire if close
    }
    public override void OnScannedBot(ScannedBotEvent evt)
    {
        if (state == 0) { // Scanning
            if (lastBotId == evt.ScannedBotId) {
                return;
            }
            if (closestBotDistance < 0 || DistanceTo(evt.X, evt.Y) < closestBotDistance) {
                closestBotDistance = DistanceTo(evt.X, evt.Y);
                closestBotId = evt.ScannedBotId;
            }
            count++;
            if (count >= EnemyCount) {
                state = 1; // Switch to chasing
            }
        } else if (state == 1) { // Chasing
            if (evt.ScannedBotId == closestBotId) {
                ChaseEnemy(evt);
            }
        }
    }
    public override void OnBotDeath(BotDeathEvent evt)
    {
        // Console.WriteLine($"Bot {evt.VictimId} died. Was it my target? {evt.VictimId == closestBotId}");
        if (evt.VictimId == closestBotId) {
            state = 0; // Scanning state
            closestBotDistance = -1;
            count = 0;
            closestBotId = -1;
            lastBotId = evt.VictimId;
        }
    }
    public override void OnHitWall(HitWallEvent evt)
    {
        // Console.WriteLine("Hit wall");
        dir = -dir; // Reverse direction
        // Console.WriteLine($"TargetSpeed: {TargetSpeed}");
    }
}
