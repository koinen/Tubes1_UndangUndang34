using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
public class Custom : Bot
{
    bool onwall = false;
    double moveAmount;

    int whichwall = 0;

    int moveCounter = 0;
    
    static void Main()
    {
        new Custom().Start();
    }

    Custom() : base(BotInfo.FromFile("Custom.json")) { }

    public override void Run()
    {
        BodyColor = Color.Black;
        TurretColor = Color.Black;
        RadarColor = Color.Black;
        BulletColor = Color.Black;
        ScanColor = Color.Black;

        moveAmount = ArenaWidth;
        onwall = false;

        TurnRight(Direction % 90); //face a wall
        Forward(moveAmount); //go to a wall

        while (IsRunning)
        {
            while (onwall == false)
            {Forward(250); //keep trying to go to wall if blocked
                onwall = OnWall(); //check if on wall
            if (onwall == true)  {
                TurnGunRight(180);
                TurnRight(90);}  //adjusts gun and rotation
                whichwall = WhichWall(); // identifies which wall for wall avoidance in oscillation
            }
            Movement(whichwall); //oscillate on wall
            TurnGunLeft(180); 
            TurnGunRight(180); //gun turn
        }
    }

private bool OnWall() //checks if bot arrived on wall (at least close to)
{
    double margin = 50.0;
    double botX = X;
    double botY = Y;
    double arenaWidth = ArenaWidth;
    double arenaHeight = ArenaHeight;
    if (botX <= margin || botX >= (arenaWidth - margin) || botY <= margin || botY >= (arenaHeight - margin)) return true;
    else return false;
}


    public override void OnScannedBot(ScannedBotEvent e) //shoot
    {
        double distance = DistanceTo(e.X, e.Y);
        double firepower = Math.Max(0.5, Math.Min(3.0, 500 / distance)); //adjusts firepower based on range

        Fire(firepower);
    }

    public override void OnHitBot(HitBotEvent e) //shoot when stuck on the way to a wall
    {
        if(onwall == false){
            Fire(2);
        }
    }


private void Movement(int whichwall)
{
    double botX = X;
    double botY = Y;
    if (DistanceRemaining == 0) // ensures last movement is done
    {
        moveCounter++;
        double moveDistance = 60 + (moveCounter % 5) * 60; //vary move distance

        
        if ((moveCounter % 2) == 0) //vary direction
        {
            if (whichwall == 1) { //wall avoidance
                if (botY + moveDistance > ArenaHeight) {
                    moveDistance = -moveDistance;
                }
            }
            if (whichwall == 2) {
                if (botY - moveDistance < 0) {
                    moveDistance = -moveDistance;
                }
            }
            if (whichwall == 3) {
                if (botX - moveDistance < 0) {
                    moveDistance = -moveDistance;
                }
            }
            if (whichwall == 4) {
                if (botX + moveDistance > ArenaWidth) {
                    moveDistance = -moveDistance;
                }
            }
            SetForward(moveDistance);
        }
        else{
            if (whichwall == 1) {
                if (botY - moveDistance < 0) {
                    moveDistance = -moveDistance;
                }
            }
            if (whichwall == 2) {
                if (botY + moveDistance > ArenaHeight) {
                    moveDistance = -moveDistance;
                }
            }
            if (whichwall == 3) {
                if (botX + moveDistance > ArenaWidth) {
                    moveDistance = -moveDistance;
                }
            }
            if (whichwall == 4) {
                if (botX - moveDistance < 0) {
                    moveDistance = -moveDistance;
                }
            }
            SetBack(moveDistance);
        }
    }
}

    private int WhichWall() { //finds which wall the bot is in
        double botX = X;
        double botY = Y;
        double W = ArenaWidth;
        double H = ArenaHeight;

        if (botX <= 50) {
            return 1; //Left
        }
        else if (botX >= (W - 50)) {
            return 2; //Right
        }
        else if (botY <= 50) {
            return 3; //Bot
        }
        else if (botY >= (H-50)){
            return 4; //Top
        }
        return 0;
    }
}


