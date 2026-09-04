namespace URWPGSim2D.Strategy
{
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using URWPGSim2D.Common;
    using URWPGSim2D.StrategyLoader;
    public class Strategy : MarshalByRefObject, IStrategy
    {
        private Decision[] decisions = null;
        List<string> strRecord = new List<string>();

        public Decision[] GetDecision(Mission mission, int teamId)
        {
            if (this.decisions == null)
            {
                this.decisions = new Decision[mission.CommonPara.FishCntPerTeam];
            }
            GlobalVar.mission = mission;
            GlobalVar.MyTeam = mission.TeamsRef[teamId];
            GlobalVar.OppTeam = mission.TeamsRef[(teamId + 1) % 2];
            for (int i = 0; i < 9; i++)
            {
                GlobalVar.balls[i] = mission.EnvRef.Balls[i];
            }
            GlobalVar.decisions = this.decisions;
            if (GlobalVar.Paishu == 0)
            {
                GlobalVar.Init();
            }
            GlobalVar.oppTeamId = (teamId + 1) % 2;
            GlobalVar.teamId = teamId;
            GlobalVar.Paishu++;
            if (GlobalVar.Paishu == 1)
            {
                GlobalVar.MyTeamLast = mission.TeamsRef[teamId];
            }
            List<RoboFish> fishes = GlobalVar.MyTeam.Fishes;
            double pI = GlobalVar.PI;
            for (int i = 0; i < 2; i++)
            {
                GlobalVar.decisions[i].VCode = 0;
                GlobalVar.decisions[i].TCode = 7;
            }
            double[,] numArray = new double[2, 2];
            double[,] numArray2 = new double[2, 2];
            double[,] numArray3 = new double[9, 2];
            for (int j = 0; j < 2; j++)
            {
                numArray[j, 0] = GlobalVar.MyTeam.Fishes[j].PositionMm.X;
                numArray[j, 1] = GlobalVar.MyTeam.Fishes[j].PositionMm.Z;
                numArray2[j, 0] = GlobalVar.MyTeam.Fishes[j].PolygonVertices[j].X;
                numArray2[j, 1] = GlobalVar.MyTeam.Fishes[j].PolygonVertices[j].Z;
            }
            for (int k = 0; k < 9; k++)
            {
                numArray3[k, 0] = GlobalVar.mission.EnvRef.Balls[k].PositionMm.X;
                numArray3[k, 1] = GlobalVar.mission.EnvRef.Balls[k].PositionMm.Z;
            }
            int num2 = Convert.ToInt32(mission.HtMissionVariables["CompetitionPeriod"]);
            int num3 = 0x176f - mission.CommonPara.RemainingCycles;
            if (GlobalVar.Stage == 4)
            {
                GlobalVar.Stage = 0;
            }
            GlobalVar.b0_l = Convert.ToInt32(mission.HtMissionVariables["Ball_0_Left_Status"]);
            GlobalVar.b1_l = Convert.ToInt32(mission.HtMissionVariables["Ball_1_Left_Status"]);
            GlobalVar.b2_l = Convert.ToInt32(mission.HtMissionVariables["Ball_2_Left_Status"]);
            GlobalVar.b3_l = Convert.ToInt32(mission.HtMissionVariables["Ball_3_Left_Status"]);
            GlobalVar.b4_l = Convert.ToInt32(mission.HtMissionVariables["Ball_4_Left_Status"]);
            GlobalVar.b5_l = Convert.ToInt32(mission.HtMissionVariables["Ball_5_Left_Status"]);
            GlobalVar.b6_l = Convert.ToInt32(mission.HtMissionVariables["Ball_6_Left_Status"]);
            GlobalVar.b7_l = Convert.ToInt32(mission.HtMissionVariables["Ball_7_Left_Status"]);
            GlobalVar.b8_l = Convert.ToInt32(mission.HtMissionVariables["Ball_8_Left_Status"]);
            GlobalVar.b0_r = Convert.ToInt32(mission.HtMissionVariables["Ball_0_Right_Status"]);
            GlobalVar.b1_r = Convert.ToInt32(mission.HtMissionVariables["Ball_1_Right_Status"]);
            GlobalVar.b2_r = Convert.ToInt32(mission.HtMissionVariables["Ball_2_Right_Status"]);
            GlobalVar.b3_r = Convert.ToInt32(mission.HtMissionVariables["Ball_3_Right_Status"]);
            GlobalVar.b4_r = Convert.ToInt32(mission.HtMissionVariables["Ball_4_Right_Status"]);
            GlobalVar.b5_r = Convert.ToInt32(mission.HtMissionVariables["Ball_5_Right_Status"]);
            GlobalVar.b6_r = Convert.ToInt32(mission.HtMissionVariables["Ball_6_Right_Status"]);
            GlobalVar.b7_r = Convert.ToInt32(mission.HtMissionVariables["Ball_7_Right_Status"]);
            GlobalVar.b8_r = Convert.ToInt32(mission.HtMissionVariables["Ball_8_Right_Status"]);
            GlobalVar.leftScore[0] = GlobalVar.b0_l;
            GlobalVar.leftScore[1] = GlobalVar.b1_l;
            GlobalVar.leftScore[2] = GlobalVar.b2_l;
            GlobalVar.leftScore[3] = GlobalVar.b3_l;
            GlobalVar.leftScore[4] = GlobalVar.b4_l;
            GlobalVar.leftScore[5] = GlobalVar.b5_l;
            GlobalVar.leftScore[6] = GlobalVar.b6_l;
            GlobalVar.leftScore[7] = GlobalVar.b7_l;
            GlobalVar.leftScore[8] = GlobalVar.b8_l;
            GlobalVar.rightScore[0] = GlobalVar.b0_r;
            GlobalVar.rightScore[1] = GlobalVar.b1_r;
            GlobalVar.rightScore[2] = GlobalVar.b2_r;
            GlobalVar.rightScore[3] = GlobalVar.b3_r;
            GlobalVar.rightScore[4] = GlobalVar.b4_r;
            GlobalVar.rightScore[5] = GlobalVar.b5_r;
            GlobalVar.rightScore[6] = GlobalVar.b6_r;
            GlobalVar.rightScore[7] = GlobalVar.b7_r;
            GlobalVar.rightScore[8] = GlobalVar.b8_r;
            switch (num3)
            {
                case 0xbb6:
                    for (int m = 0; m < 9; m++)
                    {
                        GlobalVar.leftScoreCopy[m] = GlobalVar.rightScore[m];
                        GlobalVar.rightScoreCopy[m] = GlobalVar.leftScore[m];
                    }
                    break;

                case 0xbb8:
                    for (int n = 0; n < 9; n++)
                    {
                        GlobalVar.leftScore[n] = GlobalVar.leftScoreCopy[n];
                        GlobalVar.rightScore[n] = GlobalVar.rightScoreCopy[n];
                    }
                    break;
            }
            this.RightToLeft();
            int index = GlobalVar.Bringing_BallID[0];
            int num5 = GlobalVar.Bringing_BallID[1];
            int num18 = GlobalVar.BaAction.CountBallsInMySmallArea(true, true);
            int num23 = num18 + GlobalVar.MyTeam.Para.Score;
            int num20 = GlobalVar.BaAction.CountBallsInMySmallArea(true, false);
            int score = GlobalVar.MyTeam.Para.Score;
            int num22 = num20;
            int num21 = GlobalVar.BaAction.CountBallsInMyBigArea(false, true);
            int num19 = GlobalVar.BaAction.CountBallsInOppBigArea(true, true);
            int num26 = GlobalVar.BaAction.CountBallsInOppBigArea(true, true);
            int num24 = GlobalVar.BaAction.CountBallsInOppBigArea(false, true) + num19;
            int num27 = GlobalVar.BaAction.CountBallsInOppBigArea(false, true) + num26;
            double x = GlobalVar.MyTeam.Fishes[0].PositionMm.X;
            double z = GlobalVar.MyTeam.Fishes[0].PositionMm.Z;
            double num6 = GlobalVar.MyTeam.Fishes[1].PositionMm.X;
            double num7 = GlobalVar.MyTeam.Fishes[1].PositionMm.Z;
            double velocityMmPs = GlobalVar.MyTeam.Fishes[0].VelocityMmPs;
            double num11 = GlobalVar.MyTeam.Fishes[1].VelocityMmPs;
            double angularVelocityRadPs = GlobalVar.MyTeam.Fishes[0].AngularVelocityRadPs;
            double num14 = GlobalVar.OppTeam.Fishes[0].PositionMm.X;
            double num15 = GlobalVar.OppTeam.Fishes[0].PositionMm.Z;
            double num16 = GlobalVar.OppTeam.Fishes[1].PositionMm.X;
            double num17 = GlobalVar.OppTeam.Fishes[1].PositionMm.Z;
            int num30 = GlobalVar.BaAction.CountBallsInOppMiddleArea(true, false);
            int num31 = GlobalVar.BaAction.CountBallsInOppMiddleArea(false, false) + num30;
            double num13 = GlobalVar.MyTeam.Fishes[1].AngularVelocityRadPs;
            if (num20 < 0)
            {
                num20 = 0;
            }
            if (teamId == 0)
            {
                if ((GlobalVar.balls[6].PositionMm.X < -1300f) && (Math.Abs(GlobalVar.balls[6].PositionMm.Z) > 800f))
                {
                    num22++;
                    num23++;
                }
                if ((GlobalVar.balls[3].PositionMm.X < -1300f) && (Math.Abs(GlobalVar.balls[3].PositionMm.Z) > 800f))
                {
                    num22++;
                    num23++;
                }
            }
            else
            {
                if ((GlobalVar.balls[4].PositionMm.X < -1300f) && (Math.Abs(GlobalVar.balls[6].PositionMm.Z) > 800f))
                {
                    num22++;
                    num23++;
                }
                if ((GlobalVar.balls[5].PositionMm.X < -1300f) && (Math.Abs(GlobalVar.balls[3].PositionMm.Z) > 800f))
                {
                    num22++;
                    num23++;
                }
            }
            int num25 = (0x11 - num24) - num23;
            int num28 = (0x11 - num27) - num23;
            GlobalVar.flag_duqiumen = GlobalVar.BaAction.CheckDuqiumenStatus();
            int num32 = 20;
            GlobalVar.enemyx0[GlobalVar.Paishu % num32] = GlobalVar.OppTeam.Fishes[0].PositionMm.X;
            GlobalVar.enemyz0[GlobalVar.Paishu % num32] = GlobalVar.OppTeam.Fishes[0].PositionMm.Z;
            GlobalVar.enemyx1[GlobalVar.Paishu % num32] = GlobalVar.OppTeam.Fishes[1].PositionMm.X;
            GlobalVar.enemyz1[GlobalVar.Paishu % num32] = GlobalVar.OppTeam.Fishes[1].PositionMm.Z;
            GlobalVar.enemydirection0[GlobalVar.Paishu % num32] = GlobalVar.OppTeam.Fishes[0].BodyDirectionRad;
            GlobalVar.enemydirection1[GlobalVar.Paishu % num32] = GlobalVar.OppTeam.Fishes[1].BodyDirectionRad;
            if (GlobalVar.flag_duqiumen == 1)
            {
                if (((index == -1) || ((GlobalVar.Paishu < 0xbb8) && (GlobalVar.leftScore[index] == 1))) || (GlobalVar.Paishu >= 0xbb8))
                {
                    index = GlobalVar.BaAction.ChoseBall_for_duqiumen(0);
                }
                if (((num5 == -1) || ((GlobalVar.Paishu < 0xbb8) && (GlobalVar.leftScore[num5] == 1))) || (GlobalVar.Paishu >= 0xbb8))
                {
                    num5 = GlobalVar.BaAction.ChoseBall_for_duqiumen(1);
                }
            }
            if ((GlobalVar.final == 0) || (GlobalVar.flag_duqiumen == 1))
            {
                if (GlobalVar.flag_duqiumen != 1)
                {
                    index = GlobalVar.BaAction.ChoseBall20(0);
                    num5 = GlobalVar.BaAction.ChoseBall20(1);
                }
                // 优化E：锁定正在横扫/带球的球，防止扫球途中被重选切换到其他球
                index = GlobalVar.BaAction.LockCarryingBall(0, index);
                num5 = GlobalVar.BaAction.LockCarryingBall(1, num5);
                // 优化A：已进我方球门的球重复推不重复计分，自动换球
                if (GlobalVar.BaAction.IsBallScoredInMyGoal(index))
                {
                    index = -1;
                    GlobalVar.Bringing_BallID[0] = -1;
                }
                if (GlobalVar.BaAction.IsBallScoredInMyGoal(num5))
                {
                    num5 = -1;
                    GlobalVar.Bringing_BallID[1] = -1;
                }
                if (index != -1)
                {
                    int num39 = 0;
                    if ((GlobalVar.BaAction.GetCornerSelection((double)GlobalVar.balls[index].PositionMm.X, (double)GlobalVar.balls[index].PositionMm.Z) == 1) || (GlobalVar.chooseinterfereBallStage_Up[num39] == 1))
                    {
                        GlobalVar.BaAction.ChooseCornerInterfereBall(num39, 1);
                    }
                    else if ((GlobalVar.BaAction.GetCornerSelection((double)GlobalVar.balls[index].PositionMm.X, (double)GlobalVar.balls[index].PositionMm.Z) == 2) || (GlobalVar.chooseinterfereBallStage_Down[num39] == 1))
                    {
                        GlobalVar.BaAction.ChooseCornerInterfereBall(num39, 0);
                    }
                    else if (index == 2)
                    {
                        int num44;
                        double num40 = GlobalVar.mission.EnvRef.Balls[2].PositionMm.X;
                        double num41 = GlobalVar.mission.EnvRef.Balls[2].PositionMm.Z;
                        double num42 = GlobalVar.MyTeam.Fishes[0].PolygonVertices[0].X;
                        double num43 = GlobalVar.MyTeam.Fishes[0].PolygonVertices[0].Z;
                        if (teamId == 1)
                        {
                            num44 = -1;
                        }
                        else
                        {
                            num44 = 1;
                        }
                        if (((num42 < num40) && (GlobalVar.BaAction.Compute_Distance(num40, num41, num42, num43) > 150.0)) && (num3 < 100))
                        {
                            GlobalVar.BaAction.Position(0, num40 + (num44 * 100), num41 + (num44 * 100), 350.0);
                        }
                        else if (GlobalVar.BaAction.ShouldInterceptBall(0, index))
                        {
                            GlobalVar.BaAction.InterceptBallV2(0, index);
                        }
                        else
                        {
                            GlobalVar.BaAction.DribbleOrShieldV2(0, index);
                        }
                    }
                    else if (GlobalVar.BaAction.ShouldInterceptBall(0, index))
                    {
                        GlobalVar.BaAction.InterceptBallV2(0, index);
                    }
                    else
                    {
                        GlobalVar.BaAction.DribbleOrShieldV2(0, index);
                    }
                }
                else
                {
                    // 优化B/C：空闲鱼优先抢断对方带球，其次球门威胁防守
                    int oppDribBall0 = GlobalVar.BaAction.FindOppDribblingBall();
                    if (oppDribBall0 != -1)
                    {
                        GlobalVar.BaAction.InterceptBallV2(0, oppDribBall0);
                    }
                    else
                    {
                        int threatBall0 = GlobalVar.BaAction.FindGoalThreatBall();
                        if (threatBall0 != -1)
                        {
                            GlobalVar.BaAction.DefendGoalApproach(0, threatBall0);
                        }
                        else
                        {
                            GlobalVar.BaAction.HandleBallAction(0);
                        }
                    }
                }
                if (num5 != -1)
                {
                    int num45 = 1;
                    if ((GlobalVar.BaAction.GetCornerSelection((double)GlobalVar.balls[num5].PositionMm.X, (double)GlobalVar.balls[num5].PositionMm.Z) == 1) || (GlobalVar.chooseinterfereBallStage_Up[num45] == 1))
                    {
                        GlobalVar.BaAction.ChooseCornerInterfereBall(num45, 1);
                    }
                    else if ((GlobalVar.BaAction.GetCornerSelection((double)GlobalVar.balls[num5].PositionMm.X, (double)GlobalVar.balls[num5].PositionMm.Z) == 2) || (GlobalVar.chooseinterfereBallStage_Down[num45] == 1))
                    {
                        GlobalVar.BaAction.ChooseCornerInterfereBall(num45, 0);
                    }
                    else if (num5 == 0)
                    {
                        int num50;
                        double num46 = GlobalVar.mission.EnvRef.Balls[0].PositionMm.X;
                        double num47 = GlobalVar.mission.EnvRef.Balls[0].PositionMm.Z;
                        double num48 = GlobalVar.MyTeam.Fishes[1].PolygonVertices[0].X;
                        double num49 = GlobalVar.MyTeam.Fishes[1].PolygonVertices[0].Z;
                        if (teamId == 1)
                        {
                            num50 = -1;
                        }
                        else
                        {
                            num50 = 1;
                        }
                        if (((num48 < num46) && (GlobalVar.BaAction.Compute_Distance(num46, num47, num48, num49) > 150.0)) && (num3 < 100))
                        {
                            GlobalVar.BaAction.Position(1, num46 + (num50 * 100), num47 - (num50 * 100), 350.0);
                        }
                        else if (GlobalVar.BaAction.ShouldInterceptBall(1, num5))
                        {
                            GlobalVar.BaAction.InterceptBallV2(1, num5);
                        }
                        else
                        {
                            GlobalVar.BaAction.DribbleOrShieldV2(1, num5);
                        }
                    }
                    else if (GlobalVar.BaAction.ShouldInterceptBall(1, num5))
                    {
                        GlobalVar.BaAction.InterceptBallV2(1, num5);
                    }
                    else
                    {
                        GlobalVar.BaAction.DribbleOrShieldV2(1, num5);
                    }
                }
                else
                {
                    // 优化B/C：空闲鱼优先抢断对方带球，其次球门威胁防守
                    int oppDribBall1 = GlobalVar.BaAction.FindOppDribblingBall();
                    if (oppDribBall1 != -1)
                    {
                        GlobalVar.BaAction.InterceptBallV2(1, oppDribBall1);
                    }
                    else
                    {
                        int threatBall1 = GlobalVar.BaAction.FindGoalThreatBall();
                        if (threatBall1 != -1)
                        {
                            GlobalVar.BaAction.DefendGoalApproach(1, threatBall1);
                        }
                        else
                        {
                            GlobalVar.BaAction.HandleBallAction(1);
                        }
                    }
                }
            }
            if (GlobalVar.flag_duqiumen == 0)
            {
                if ((GlobalVar.Paishu == 0xbb7) || (GlobalVar.Paishu == 0xbb8))
                {
                    GlobalVar.final = 1;
                    GlobalVar.CQstage = 0;
                    GlobalVar.lanqiuFlag[0] = 0;
                    GlobalVar.lanqiuFlag[1] = 0;
                    GlobalVar.Bringing_BallID[0] = -1;
                    GlobalVar.Bringing_BallID[1] = -1;
                    GlobalVar.escape_lanqiu = 0;
                }
                if (((GlobalVar.Paishu < 0xbb7) && (GlobalVar.Paishu > 0x8fc)) && (GlobalVar.finallanqiu == 0))
                {
                    GlobalVar.lanqiuFlag[0] = 1;
                    GlobalVar.lanqiuFlag[1] = 1;
                }
                if ((GlobalVar.Paishu > 0xbc2) && (GlobalVar.finallanqiu == 0))
                {
                    GlobalVar.lanqiuFlag[0] = 1;
                    GlobalVar.lanqiuFlag[1] = 1;
                }
                if (score >= 9)
                {
                    GlobalVar.CQstage = 1;
                }
                if (((num18 > 8) || (num23 > 10)) || (GlobalVar.final == 1))
                {
                    GlobalVar.final = 1;
                    if ((GlobalVar.lanqiuFlag[0] != 0) || (GlobalVar.lanqiuFlag[1] > 0))
                    {
                        GlobalVar.finallanqiu = 0;
                    }
                    if ((((num20 == 0) && (GlobalVar.finallanqiu == 1)) && (GlobalVar.lanqiuFlag[0] == 0)) && (GlobalVar.lanqiuFlag[1] == 0))
                    {
                        GlobalVar.finallanqiu = 0;
                        GlobalVar.lanqiuFlag[1] = 1;
                        GlobalVar.lanqiuFlag[0] = 1;
                    }
                    if (((((num22 > 2) || (GlobalVar.finallanqiu == 1)) && ((GlobalVar.lanqiuFlag[0] == 0) && (GlobalVar.lanqiuFlag[1] == 0))) && ((GlobalVar.Paishu < 0x1388) && (num23 > num24))) && (GlobalVar.escape_lanqiu == 0))
                    {
                        GlobalVar.BaAction.Dribblehometestlast2min(0);
                        GlobalVar.BaAction.Dribblehometestlast2min(1);
                        GlobalVar.finallanqiu = 1;
                        GlobalVar.Bringing_BallID[0] = -1;
                        GlobalVar.Bringing_BallID[1] = -1;
                    }
                    else if ((num20 >= 1) && (GlobalVar.CQstage == 0))
                    {
                        GlobalVar.escape_lanqiu = 1;
                        index = GlobalVar.BaAction.choseball3_special(0);
                        num5 = GlobalVar.BaAction.ChoseBall2(1);
                        if (index != -1)
                        {
                            GlobalVar.BaAction.the_warrior2(0, index);
                        }
                        else
                        {
                            GlobalVar.CQstage = 1;
                        }
                        if (num5 != -1)
                        {
                            int whichfish = 1;
                            // 优化D：终局阶段复用预测拦截
                            if (GlobalVar.BaAction.ShouldInterceptBall(1, num5))
                            {
                                GlobalVar.BaAction.InterceptBallV2(1, num5);
                            }
                            else
                            {
                                GlobalVar.BaAction.DribbleOrShieldV2(whichfish, num5);
                            }
                        }
                        else
                        {
                            GlobalVar.BaAction.HandleBallAction(1);
                        }
                    }
                    else
                    {
                        GlobalVar.escape_lanqiu = 1;
                        GlobalVar.CQstage = 1;
                        index = GlobalVar.BaAction.ChoseBallplus(0);
                        num5 = GlobalVar.BaAction.ChoseBallplus(1);
                        if (index != -1)
                        {
                            if (GlobalVar.BaAction.IsInMyUnArea_01((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X))
                            {
                                GlobalVar.BaAction.the_warrior2(0, index);
                            }
                            else if (GlobalVar.BaAction.ShouldInterceptBall(0, index))
                            {
                                GlobalVar.BaAction.InterceptBallV2(0, index);
                            }
                            else
                            {
                                GlobalVar.BaAction.DribbleOrShieldV2(0, index);
                            }
                        }
                        else
                        {
                            GlobalVar.BaAction.HandleBallAction(0);
                        }
                        if (num5 != -1)
                        {
                            if (GlobalVar.BaAction.IsInMyUnArea_01((double)GlobalVar.mission.EnvRef.Balls[num5].PositionMm.X))
                            {
                                GlobalVar.BaAction.the_warrior2(1, num5);
                            }
                            else if (GlobalVar.BaAction.ShouldInterceptBall(1, num5))
                            {
                                GlobalVar.BaAction.InterceptBallV2(1, num5);
                            }
                            else
                            {
                                GlobalVar.BaAction.DribbleOrShieldV2(1, num5);
                            }
                        }
                        else
                        {
                            GlobalVar.BaAction.HandleBallAction(1);
                        }
                    }
                }
            }
            if (GlobalVar.flag_duqiumen != 1)
            {
                for (int num52 = 0; num52 < num32; num52++)
                {
                    if ((GlobalVar.enemyx0[num52] >= -1350.0) || (((GlobalVar.enemydirection0[num52] <= ((GlobalVar.PI * -175.0) / 180.0)) || (GlobalVar.enemydirection0[num52] >= ((GlobalVar.PI * -140.0) / 180.0))) && ((GlobalVar.enemydirection0[num52] <= ((GlobalVar.PI * 140.0) / 180.0)) || (GlobalVar.enemydirection0[num52] >= ((GlobalVar.PI * 175.0) / 180.0)))))
                    {
                        GlobalVar.UnarmedFlag0 = 0;
                        break;
                    }
                    GlobalVar.UnarmedFlag0 = 1;
                }
                for (int num53 = 0; num53 < num32; num53++)
                {
                    if ((GlobalVar.enemyx1[num53] >= -1350.0) || (((GlobalVar.enemydirection1[num53] <= ((GlobalVar.PI * -175.0) / 180.0)) || (GlobalVar.enemydirection1[num53] >= ((GlobalVar.PI * -140.0) / 180.0))) && ((GlobalVar.enemydirection1[num53] <= ((GlobalVar.PI * 140.0) / 180.0)) || (GlobalVar.enemydirection1[num53] >= ((GlobalVar.PI * 175.0) / 180.0)))))
                    {
                        GlobalVar.UnarmedFlag1 = 0;
                        break;
                    }
                    GlobalVar.UnarmedFlag1 = 1;
                }
                if (((((GlobalVar.UnarmedFlag0 == 1) && (GlobalVar.BaAction.CountBallsInMySmallArea(true, false) > 0)) && (!GlobalVar.BaAction.IsInMyUnArea(x, z) && !GlobalVar.BaAction.IsInMyUnArea(num6, num7))) && ((GlobalVar.final == 0) && (GlobalVar.enemydirection0[10] > ((GlobalVar.PI * -175.0) / 180.0)))) && (GlobalVar.enemydirection0[10] < ((GlobalVar.PI * -140.0) / 180.0)))
                {
                    GlobalVar.UnarmedStage = 1;
                }
                if (((((GlobalVar.UnarmedFlag0 == 1) && (GlobalVar.BaAction.CountBallsInMySmallArea(true, false) > 0)) && (!GlobalVar.BaAction.IsInMyUnArea(x, z) && !GlobalVar.BaAction.IsInMyUnArea(num6, num7))) && ((GlobalVar.final == 0) && (GlobalVar.enemydirection0[10] > ((GlobalVar.PI * 140.0) / 180.0)))) && (GlobalVar.enemydirection0[10] < ((GlobalVar.PI * 175.0) / 180.0)))
                {
                    GlobalVar.UnarmedStage = 2;
                }
                if (((((GlobalVar.UnarmedFlag1 == 1) && (GlobalVar.BaAction.CountBallsInMySmallArea(true, false) > 0)) && (!GlobalVar.BaAction.IsInMyUnArea(x, z) && !GlobalVar.BaAction.IsInMyUnArea(num6, num7))) && ((GlobalVar.final == 0) && (GlobalVar.enemydirection1[10] > ((GlobalVar.PI * -175.0) / 180.0)))) && (GlobalVar.enemydirection1[10] < ((GlobalVar.PI * -140.0) / 180.0)))
                {
                    GlobalVar.UnarmedStage = 3;
                }
                if (((((GlobalVar.UnarmedFlag1 == 1) && (GlobalVar.BaAction.CountBallsInMySmallArea(true, false) > 0)) && (!GlobalVar.BaAction.IsInMyUnArea(x, z) && !GlobalVar.BaAction.IsInMyUnArea(num6, num7))) && ((GlobalVar.final == 0) && (GlobalVar.enemydirection1[10] > ((GlobalVar.PI * 140.0) / 180.0)))) && (GlobalVar.enemydirection1[10] < ((GlobalVar.PI * 175.0) / 180.0)))
                {
                    GlobalVar.UnarmedStage = 4;
                }
                if (((((GlobalVar.UnarmedStage == 1) || (GlobalVar.UnarmedStage == 2)) && (Math.Abs(GlobalVar.OppTeam.Fishes[0].PositionMm.Z) >= 600f)) || (((GlobalVar.UnarmedStage == 3) || (GlobalVar.UnarmedStage == 4)) && (Math.Abs(GlobalVar.OppTeam.Fishes[1].PositionMm.Z) >= 600f))) || (GlobalVar.final == 1))
                {
                    GlobalVar.UnarmedStage = 0;
                }
                if (GlobalVar.UnarmedStage > 0)
                {
                    if (GlobalVar.UnarmedStage == 1)
                    {
                        int num54 = GlobalVar.BaAction.choseball3_special2(1);
                        if (num54 != -1)
                        {
                            GlobalVar.BaAction.the_warrior2_special(1, num54);
                        }
                        else
                        {
                            GlobalVar.UnarmedStage = 0;
                        }
                    }
                    else if (GlobalVar.UnarmedStage == 2)
                    {
                        int num55 = GlobalVar.BaAction.choseball3_special2(0);
                        if (num55 != -1)
                        {
                            GlobalVar.BaAction.the_warrior2_special(0, num55);
                        }
                        else
                        {
                            GlobalVar.UnarmedStage = 0;
                        }
                    }
                    else if (GlobalVar.UnarmedStage == 3)
                    {

                        int num56 = GlobalVar.BaAction.choseball3_special2(1);
                        if (num56 != -1)
                        {
                            GlobalVar.BaAction.the_warrior2_special(1, num56);
                        }
                        else
                        {
                            GlobalVar.UnarmedStage = 0;
                        }
                    }
                    else if (GlobalVar.UnarmedStage == 4)
                    {
                        int num57 = GlobalVar.BaAction.choseball3_special2(0);
                        if (num57 != -1)
                        {
                            GlobalVar.BaAction.the_warrior2_special(0, num57);
                        }
                        else
                        {
                            GlobalVar.UnarmedStage = 0;
                        }
                    }
                }
            }
            if (((angularVelocityRadPs < 0.08) && (velocityMmPs < 20.0)) && (GlobalVar.finallanqiu == 0))
            {
                GlobalVar.decisions[0].VCode = 12;
            }
            if (((num13 < 0.08) && (num11 < 20.0)) && (GlobalVar.finallanqiu == 0))
            {
                GlobalVar.decisions[1].VCode = 12;
            }
            // 问题1修复：撞墙脱困（覆盖上方防呆滞的顶墙行为）
            GlobalVar.BaAction.UnstickFish(0);
            GlobalVar.BaAction.UnstickFish(1);
            GlobalVar.MyTeamLast = mission.TeamsRef[teamId];
            return this.decisions;
        }

        public string GetTeamName()
        {
            return " NEU ";
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void RightToLeft()
        {
            if (GlobalVar.teamId != 0)
            {
                for (int i = 0; i < 9; i++)
                {
                    GlobalVar.balls[i].PositionMm.X = -GlobalVar.balls[i].PositionMm.X;
                    GlobalVar.balls[i].PositionMm.Z = -GlobalVar.balls[i].PositionMm.Z;
                    GlobalVar.balls[i].VelocityDirectionRad = (float)GlobalVar.BaAction.FormatAngleRad(GlobalVar.balls[i].VelocityDirectionRad + GlobalVar.PI);
                }
                GlobalVar.leftScore[0] = GlobalVar.b0_r;
                GlobalVar.leftScore[1] = GlobalVar.b1_r;
                GlobalVar.leftScore[2] = GlobalVar.b2_r;
                GlobalVar.leftScore[3] = GlobalVar.b3_r;
                GlobalVar.leftScore[4] = GlobalVar.b4_r;
                GlobalVar.leftScore[5] = GlobalVar.b5_r;
                GlobalVar.leftScore[6] = GlobalVar.b6_r;
                GlobalVar.leftScore[7] = GlobalVar.b7_r;
                GlobalVar.leftScore[8] = GlobalVar.b8_r;
                GlobalVar.rightScore[0] = GlobalVar.b0_l;
                GlobalVar.rightScore[1] = GlobalVar.b1_l;
                GlobalVar.rightScore[2] = GlobalVar.b2_l;
                GlobalVar.rightScore[3] = GlobalVar.b3_l;
                GlobalVar.rightScore[4] = GlobalVar.b4_l;
                GlobalVar.rightScore[5] = GlobalVar.b5_l;
                GlobalVar.rightScore[6] = GlobalVar.b6_l;
                GlobalVar.rightScore[7] = GlobalVar.b7_l;
                GlobalVar.rightScore[8] = GlobalVar.b8_l;
                List<RoboFish> fishes = GlobalVar.MyTeam.Fishes;
                List<RoboFish> list2 = GlobalVar.OppTeam.Fishes;
                for (int j = 0; j < fishes.Count; j++)
                {
                    fishes[j].PositionMm.X = -fishes[j].PositionMm.X;
                    fishes[j].PositionMm.Z = -fishes[j].PositionMm.Z;
                    Vector3 vector = new Vector3
                    {
                        X = -fishes[j].PolygonVertices[0].X,
                        Z = -fishes[j].PolygonVertices[0].Z
                    };
                    fishes[j].PolygonVertices[0] = vector;
                    // 修复：镜像时应翻转各自的方向量（原代码误用旧速度方向覆盖身体方向，导致换边后转向/带球时方向错乱）
                    fishes[j].BodyDirectionRad = (float)GlobalVar.BaAction.FormatAngleRad(fishes[j].BodyDirectionRad + GlobalVar.PI);
                    fishes[j].VelocityDirectionRad = (float)GlobalVar.BaAction.FormatAngleRad(fishes[j].VelocityDirectionRad + GlobalVar.PI);
                }
                for (int k = 0; k < list2.Count; k++)
                {
                    list2[k].PositionMm.X = -list2[k].PositionMm.X;
                    list2[k].PositionMm.Z = -list2[k].PositionMm.Z;
                    Vector3 vector2 = new Vector3
                    {
                        X = -list2[k].PolygonVertices[0].X,
                        Z = -list2[k].PolygonVertices[0].Z
                    };
                    list2[k].PolygonVertices[0] = vector2;
                    // 修复：对方鱼镜像同样翻转各自的方向量
                    list2[k].VelocityDirectionRad = (float)GlobalVar.BaAction.FormatAngleRad(list2[k].VelocityDirectionRad + GlobalVar.PI);
                    list2[k].BodyDirectionRad = (float)GlobalVar.BaAction.FormatAngleRad(list2[k].BodyDirectionRad + GlobalVar.PI);
                }
            }
        }
    }
}

