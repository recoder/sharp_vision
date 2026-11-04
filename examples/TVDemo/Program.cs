// Ported from examples/tvdemo/tvdemo1.cpp (main).

using SharpVision.Objects;

var demoProgram = new TVDemo(args);
demoProgram.Run();
TObject.Destroy(demoProgram);
