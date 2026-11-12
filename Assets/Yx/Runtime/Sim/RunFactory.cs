namespace Yx.Sim
{
    public static class RunFactory
    {
        public static RunState New(int seed, float maxAir, bool tutorial, int unlockedCount)
        {
            if (seed == 0) seed = 1;
            if (maxAir < 30f) maxAir = 30f;
            var run = new RunState();
            run.Seed = seed;
            run.Rng = new Rng((uint)seed);
            run.MaxAir = maxAir;
            run.Air = maxAir;
            run.Life = 3f;
            run.Tutorial = tutorial;
            run.UnlockedCount = unlockedCount < Rules.StartModules ? Rules.StartModules : unlockedCount;
            if (run.UnlockedCount > 12) run.UnlockedCount = 12;
            run.FaceX = 0f;
            run.FaceY = 1f;
            for (int i = 0; i < 4; i++)
                run.Rooms.Add(RoomBuilder.BuildSurvey(run, i, tutorial && i == 0));
            Room boss = RoomBuilder.BuildBoss(run);
            RoomBuilder.AddBoss(run, boss);
            run.Rooms.Add(boss);
            Room start = run.Rooms[0];
            run.PlayerX = start.SpawnX;
            run.PlayerY = start.SpawnY;
            run.MaxRoomReached = 1;
            return run;
        }
    }
}
