using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using System.Collections.Generic;
using System;

// ------------------------------------------------------------------
// Perpendicular
// ------------------------------------------------------------------
//
// Bot ini mengambil satu musuh terdekat dan mengejar musuh, bergerak tegak lurus terhadap musuh jika cukup dekat.
// Bot ini akan menembak musuh dengan firepower tergantung jarak musuh dan arah tergantung kecepatan lateral musuh.
// ------------------------------------------------------------------

public class Perpendicular : Bot
{
    int state, count, dir = 1;
    int closestBotId, lastBotId;
    double closestBotDistance;
    static void Main(string[] args) { new Perpendicular().Start(); }
    
    Perpendicular() : base(BotInfo.FromFile("Perpendicular.json")) { }

    public override void Run()
    {   
        state = 0; // Scanning state
        closestBotDistance = -1;
        count = 0;
        closestBotId = -1;
        lastBotId = -1;
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
        double enemyLateralSpeed = e.Speed * Math.Sin(e.Direction - (turn + Direction) * Math.PI/180);
        double overcompensate = Speed == 0 ? 0 : enemyLateralSpeed/Speed;

        // Adjust TargetSpeed based on distance
        var rd = new Random();
        if (rd.NextDouble() > 0.9) { // Randomly change speed
            MaxSpeed = rd.NextDouble() * 10 + 4;
        }
        if (enemyDistance > 200) { // Enemy is far, move towards it
            SetTurnLeft(turn + overcompensate + Math.Max((90 - (Math.Abs(enemyDistance - 100) * 2)), 0)); // ensures smooth turn when distance reaches 100
            if (enemyDistance > 200) {
                dir = 1;
            }
            SetTurnGunLeft(gunTurn + enemyLateralSpeed*(800/enemyDistance));
            SetForward(100 * dir);
            SetFire(0.7);
        } else if (enemyDistance >= 50) { // Enemy is close, move perpendicular
            double perpendicular = NormalizeRelativeAngle(BearingTo(e.X, e.Y) + 90);
            SetTurnLeft(perpendicular + overcompensate);
            SetForward(100 * dir);
            SetTurnGunLeft(gunTurn + enemyLateralSpeed*(200/enemyDistance));
            SetFire(1.5);
        } else { // Enemy is too close, back off
            SetTurnLeft(turn);
            SetTurnGunLeft(gunTurn);
            SetBack(100);
            SetFire(3);
        }
    }
    public override void OnScannedBot(ScannedBotEvent evt)
    {
        if (state == 0) { // Scanning
            if (lastBotId == evt.ScannedBotId) { // prevent scanning the same bot after bot death
                return;
            }
            if (closestBotDistance < 0 || DistanceTo(evt.X, evt.Y) < closestBotDistance) {
                closestBotDistance = DistanceTo(evt.X, evt.Y);
                closestBotId = evt.ScannedBotId;
            }
            count++;
            if (count >= EnemyCount) { // If we have scanned enough enemies, switch to chasing
                state = 1;
            }
        } else if (state == 1) { // Chasing
            if (evt.ScannedBotId == closestBotId) {
                ChaseEnemy(evt);
            }
        }
    }

    public override void OnHitBot(HitBotEvent e)
    {
        if (e.IsRammed) // If we rammed the enemy, switch target
        {
            if (closestBotId == e.VictimId) {
                return;
            }
            lastBotId = closestBotId;
            closestBotId = e.VictimId;
            state = 1;
        }
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        if (evt.VictimId == closestBotId) { // If the bot we were chasing died, switch target
            state = 0; // Scanning state
            closestBotDistance = -1;
            count = 0;
            closestBotId = -1;
            lastBotId = evt.VictimId;
        }
    }
    public override void OnHitWall(HitWallEvent evt)
    {
        dir = -dir; // Reverse direction
    }
}
