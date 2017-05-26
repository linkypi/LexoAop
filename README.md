# LexoAop
使用 mono.cecil 实现编译时Aop
- Leox.Aop  使用Aop功能时必须继承里面的基类

- Leox.Injector 注入IL代码的实现

- Leox.BuildTask  MSBuild的实现，即在使用到的项目的csproj中加入以下内容来实现生成后执行该Task，达到注入的功能。
``` xml
  <PropertyGroup>
  <MyTaskDirectory>libs\</MyTaskDirectory>
  </PropertyGroup>
  <!--UsingTask中的TaskName一定要对应类名-->
  <UsingTask TaskName="AopBuildTask" AssemblyFile="$(MyTaskDirectory)Leox.BuildTask.dll" />
  <Target Name="BeforeBuild">
  </Target>
  <Target Name="AfterBuild">
    <AopBuildTask OutputFile="$(MSBuildProjectFullPath)" TaskFile="Leox.Injector.exe">
      <Output PropertyName="path" TaskParameter="Paths" />
    </AopBuildTask>
    <Message Text="build path: $(path)" />
  </Target>
```
- Leox.AopBuildTest 测试Aop 
使用的是先继承基类，然后直接以Attribute的方式使用即可
``` c#
    public class Log : MethodAspect
    {
        public override void OnStart()
        {
            Console.WriteLine("log start");
        }

        public override void OnEnd()
        {
            Console.WriteLine("log end");
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
          Say("nice,well done.")
        }
        [Log]
        public static void Say(string words) {
           Console.WriteLine(words);
        }
    }
  ```
