using System;
using Starfall;
public static class GuidanceTestMain {
 public static int Main(){int passed=0,failed=0;GuidanceRuleChecks.Run((name,ok)=>{Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(ok)passed++;else failed++;});Console.WriteLine("GUIDANCE_RULES "+passed+" PASS / "+failed+" FAIL");return failed==0?0:2;}
}
