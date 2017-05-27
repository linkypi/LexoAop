# LexoAop
使用 mono.cecil 实现编译时Aop
- Leox.Aop  使用Aop功能时必须继承里面的基类

- Leox.Injector 注入IL代码的实现，通过在项目属性中切换输出类型来生成dll或者注入工具类exe.
  如果大家觉得IL代码实在难写，那就用一种笨办法，就是首先写好对应的C#代码，
  编译通过后用ILSpy或者ildasm来查看对应的那部分il代码，然后只要照着里面的来写就OK。
  Injector.cs中有一个 MethodCache 类已无用，因为缓存是死的，在下次注入时模块的版本guid与上次注入模块的版本guid已不同，
  所以将旧版本的方法注入到新的模块会报错。

- Leox.BuildTask  自定义一个MSBuild Task，即在使用到的项目的csproj中加入以下内容来实现在指定项目生成后执行该Inject Task，达到注入的功能。
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
 若要调试BuidTask项目请在 项目属性-> 调试 做以下配置
 1. 启动操作 -> 选中启动外部程序，输入 C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe ，
   注意区分32、64位的Framework，如果填错则会报错无法调试
 2. 命令行参数输入： BuildSample.proj /fl /flp:v=diag ，BuildSample.proj 必须有
 3. 工作目录填写该项目对应的bin/Debug全路径
- Leox.AopBuildTest 测试Aop 
使用方式是先继承基类，然后直接以Attribute的方式使用即可
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
  这里有个问题是在执行BuildTask任务时，MSBuild会将libs中用到的dll锁定，
  如果想更新那就得先把MSBuild进程关掉才可以更新，如果觉得这样太麻烦可以设置
  环境变量MSBUILDDISABLENODEREUSE的值设置为 1 ，这样MSBuild进程就不会长期留在内存中
