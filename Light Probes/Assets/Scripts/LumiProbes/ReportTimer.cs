class ReportTimer
{
    public float totalTime = 0.0f;
    public long tetrahedralizeTime = 0;
    public long mappingTime = 0;
    public long step1Time = 0;
    public long step2Time = 0;
    public long step3Time = 0;
    public long step4Time = 0;
    public long step5Time = 0;
    public long step6Time = 0;

    public ReportTimer() {
        Reset();
    }

    public void Report(int num_iterations, double error) {
        LumiLogger.Logger.Log("Finished after " + num_iterations.ToString() + " iterations. Final error: " + error.ToString("0.00") + "%." +
        ", Stages Time: 1. Remove LP: " + step1Time / 1000.0 + "s" + 
        ", 2. Remap EPs: " + step2Time / 1000.0 + "s" +
        ", 2.1 Tetrahed: " + tetrahedralizeTime / 1000.0 + "s" +
        ", 2.2 Mappings: " + mappingTime / 1000.0 + "s" +
        ", 3. Eval  EPs: " + step3Time / 1000.0 + "s" +
        ", 4. Calc Cost: " + step4Time / 1000.0 + "s" +
        ", 5. Find Min : " + step5Time / 1000.0 + "s" +
        ", 6. Insert LP: " + step6Time / 1000.0 + "s" +
        ", Total: " + (step1Time + step2Time + step3Time + step4Time + step5Time + step6Time) / 1000.0 + "s");
    }

    public void Reset() {
        totalTime = 0.0f;
        tetrahedralizeTime = 0;
        mappingTime = 0;
        step1Time = 0;
        step2Time = 0;
        step3Time = 0;
        step4Time = 0;
        step5Time = 0;
        step6Time = 0;
    }
}