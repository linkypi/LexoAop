# LexoAop

此项目是使用 mono.cecil 实现的编译时Aop。
##### 1. 使用方式
  &emsp; &emsp;先继承基类 MethodAspect(MethodAspect 继承于 Attribute )，然后直接以Attribute的方式使用即可
``` c#
    class Program
    {
        static void Main(string[] args)
        {
          var count = Write("nice,well done.")
		  Console.WriteLine(string.Format("your had write {0} words",count.ToString()));
        }
		
		[Log(Order = 1, ExceptionStrategy = ExceptionStrategy.UnThrow), Timing(Order = 2)]
        public static int Write(string title, string words) {
           Console.WriteLine(string.Format("title: {0}, content: {1}",title,words));
		   return string.IsNullOrEmpty(words)? 0 : words.Length;
        }
    }
	
    class Timing : MethodAspect
    {
        public override void OnStart(MethodAspectArgs args)
        {
		    Console.WriteLine("timing start");
			Console.WriteLine("method args: ");
            if (args != null && args.Argument != null) {
                foreach (var item in args.Argument)
                {
                    Console.Write(item.ToString() + "  ");
                }
            }
        }

        public override void OnEnd(MethodAspectArgs args)
        {
            Console.WriteLine("timing end");
        }

        public override void OnSuccess(MethodAspectArgs args)
        {
            Console.WriteLine("timing success : " + args.ReturnValue.ToString());
        }
		
	    public override void OnException(MethodAspectArgs args)
        {
            Console.WriteLine("timing on exception: " + args.Exception.Message);
        }
    }
	
	class Log : MethodAspect
    {
        public override void OnException(MethodAspectArgs args)
        {
            Console.WriteLine("log on exception: " + args.Exception.Message);
        }
        public override void OnStart(MethodAspectArgs args)
        {
            Console.WriteLine("log start");
        }

        public override void OnEnd(MethodAspectArgs args)
        {
            Console.WriteLine("log end");
        }

        public override void OnSuccess(MethodAspectArgs args)
        {
            Console.WriteLine("log success. ");
			if(args.ReturnValue!= null)
				Console.WriteLine("log success, return value : " + args.ReturnValue.ToString()); 
        }
    }
```

##### 2. Leox.Aop , Aop基类，如方法拦截基类 MethodAspect，异常处理策略 ExceptionStrategy

##### 3. Leox.Injector 注入IL代码的实现，
  &emsp; &emsp;通过在项目属性中切换输出类型来生成dll或者注入工具类exe.目前实现的是方法级别的拦截，基本思路:
  - 加载程序集，找到标记有MethodAspect Attribute的方法
  - 复制该方法并生成一个新的方法copy_method，复制完成后清楚原有方法
  - 改写原有方法，首先调用AopAttribute的Start方法
  - 执行copy_method ，如果该方法是实例方法则需要ldarg0加载this，执行成功则执行 OnSuccess 方法
  - copy_method 执行出错则捕获异常，异常处理提供三种处理方法，
    一种是只捕获不抛出即 UnThrow ，第二种是抛出这个ex 即ThrowNew，
	最后一种是 ReThrow（默认） 即代码 ``` c# throw ```，保存了完整的异常跟踪链 
	如果有多个AopAttribute指定了ExceptionStrategy属性，则只会以Order最小的那个为准，即排在最前面的为准
	  异常处理的方式有以下三种
    ``` c#
	    public enum ExceptionStrategy
		{
			UnThrow,
			/// <summary>
			/// throw ex; 
			/// </summary>
			ThrowNew,
			/// <summary>
			/// throw; //保存有异常跟踪链
			/// </summary>
			ReThrow
		}
    ``` 
  - 以上处理完成最后调用AopAttribute的OnEnd方法
  
  对用值类型转引用类型时记得装箱Box.
  如果大家觉得IL代码实在难写，那就用一种笨办法，就是首先写好对应的C#代码，
  编译通过后用ILSpy或者ildasm来查看对应的那部分il代码，然后只要照着里面的来写就OK。
  Injector.cs中有一个 MethodCache 类已无用，因为缓存是死的，在下次注入时模块的版本
  guid与上次注入模块的版本guid已不同，所以将旧版本的方法注入到新的模块会报错。
  
  很多时候注入之后会发生一些未知的错误，也不知道该怎么去调试，还要返回到自己写的c# 代码一行行查看IL指令，
  这样就非常的蛋疼。所以这里推荐一个IL级别代码调试工具 -- dotnet il editor.
  使用方式是
  - 新建一个控制台项目，在执行指定方法前加一行 ```  Console.ReadKey(); ``` ，然后生成exe，也就是编译注入
  - 然后打开该exe，这时进程就暂停了，需要输入字符，但是这个时候还不能输入
  - 这时候打开 dotnet il editor，将该exe拖动到project explorer，找到你要执行的il代码按F9打断点
  - 然后在这个editor找到 Debug -> Attach to process... 找到刚刚打开的exe进程后点OK
  - 这时候返回到exe窗口随便输入一个字符就可以调试IL了
  
  注入后的代码大概长这样：
  
   ``` c#
	[Log(Order = 1, ExceptionStrategy = ExceptionStrategy.UnThrow), Timing(Order = 12)]
	public int Say(string words, int age)
	{
		MemberInfo method = typeof(Service).GetMethod("Say", new Type[]
		{
			typeof(string),
			typeof(int)
		});
		MethodAspectArgs methodAspectArgs = new MethodAspectArgs();
		methodAspectArgs.Argument = new object[]
		{
			words,
			age
		};
		MAList mAList = new MAList();
		Log item = method.GetCustomAttributes(typeof(Log), false)[0] as Log;
		mAList.Add(item);
		Timing item2 = method.GetCustomAttributes(typeof(Timing), false)[0] as Timing;
		mAList.Add(item2);
		foreach (MethodAspect current in mAList)
		{
			current.OnStart(methodAspectArgs);
		}
		int num;
		try
		{
			num = this._Say_(words, age);
			methodAspectArgs.ReturnValue = num;
			foreach (MethodAspect current in mAList)
			{
				current.OnSuccess(methodAspectArgs);
			}
		}
		catch (Exception ex)
		{
			methodAspectArgs.Exception = ex;
			foreach (MethodAspect current in mAList)
			{
				current.OnException(methodAspectArgs);
			}
			switch ((int)mAList[0].ExceptionStrategy)
			{
			case 1:
				throw ex;
			case 2:
				throw;
			}
		}
		foreach (MethodAspect current in mAList)
		{
			current.OnEnd(methodAspectArgs);
		}
		return num;
	}
   ```

##### 4. Leox.BuildTask 
&emsp; &emsp; 自定义一个MSBuild Task，即在使用到的项目的csproj中加入以下内容来实现在指定项目
生成后执行该Inject Task，达到注入的目的。
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
 
##### 5. Leox.AopBuildTest 
&emsp; &emsp;引用BuildTask 项目测试Aop 项目，这里有个问题是在执行BuildTask任务时，MSBuild会将libs中用到的dll锁定，
  如果想更新那就得先把MSBuild进程关掉才可以更新，如果觉得这样太麻烦可以设置
  环境变量MSBUILDDISABLENODEREUSE的值设置为 1 ，这样MSBuild进程就不会长期留在内存中
  
##### 6. 遗留问题:
&emsp; &emsp; 程序集被注入后无法使用VS来调试，如果哪位朋友知道的话麻烦告诉我一声，不胜感激！

&emsp; &emsp; 个人博客:  [http://blog.magicleox.com/](http://blog.magicleox.com/)











