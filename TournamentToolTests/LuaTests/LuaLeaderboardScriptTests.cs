using MoonSharp.Interpreter;
using NuGet.Versioning;
using TournamentTool.Modules.Lua;
using TournamentTool.Utils.Exceptions;

namespace TournamentToolTests.LuaTests;

public class LuaLeaderboardScriptTests
{
    private string WriteTempScript(string code)
    {
        var path = Path.GetTempFileName() + ".lua";
        File.WriteAllText(path, code);
        return path;
    }

    private class TestContext
    {
        public bool Called = false;

        public void test_method()
        {
            Called = true;
        }
    }

    [Fact]
    public void GetDescription_ShouldReturnExpectedText()
    {
        string lua = @"
            description = 'Leaderboard v1 script'
            version = '1.0.0'
            function evaluate_data(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.Equal("Leaderboard v1 script", script.Description);
    }

    [Fact]
    public void GetDescription_ShouldReturnEmpty_IfNotDefined()
    {
        string lua = @"
            version = '1.0.0'
            function evaluate_data(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.Equal(string.Empty, script.Description);
    }

    [Fact]
    public void GetDescription_ShouldReturnEmpty_IfDescriptionIsNil()
    {
        string lua = @"
            description = nil
            version = '1.0.0'
            function evaluate_data(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.Equal(string.Empty, script.Description);
    }

    [Fact]
    public void GetVersion_ShouldReturnCorrectVersion()
    {
        string lua = @"
            version = '2.5.1'
            function evaluate_data(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.Equal(new NuGetVersion("2.5.1"), script.Version);
    }

    [Fact]
    public void GetVersion_ShouldReturnNull_IfNotDefined()
    {
        string lua = @"
            description = 'some script'
            function evaluate_data(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.Null(script.Version);
    }

    [Fact]
    public void GetVersion_ShouldReturnNull_IfEmpty()
    {
        string lua = @"
            version = ''
            function evaluate_data(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.Null(script.Version);
    }

    [Fact]
    public void GetVersion_ShouldReturnNull_IfInvalid()
    {
        string lua = @"
            version = 'not_a_valid_version'
            function evaluate_data(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.Null(script.Version);
    }

    [Fact]
    public void Run_ShouldCallEvaluatePlayer()
    {
        string lua = @"
            function evaluate_data(api)
                api:test_method()
            end
        ";
        var path = WriteTempScript(lua);
        UserData.RegisterType<TestContext>();
        var script = LuaLeaderboardScript.Load(path, string.Empty, null, false);

        var context = new TestContext();
        script.Run(context);

        Assert.True(context.Called);
    }

    [Fact]
    public void Run_ShouldNotThrow_IfEvaluatePlayerIsEmpty()
    {
        string lua = @"
            function evaluate_data(api)
            end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        var context = new TestContext();
        var ex = Record.Exception(() => script.Run(context));

        Assert.Null(ex);
    }

    [Fact]
    public void Load_ShouldThrow_IfLuaIsInvalid()
    {
        string lua = "this is not valid Lua!";
        var path = WriteTempScript(lua);

        Assert.Throws<LuaScriptValidationException>(() => LuaLeaderboardScript.Load(path));
    }

    [Fact]
    public void Validate_ShouldCaptureSyntaxError()
    {
        string lua = "function evaluate_data(api) if true then return end"; // missing 'end'
        var path = WriteTempScript(lua);

        var script = new LuaLeaderboardScript(File.ReadAllText(path), path);
        var result = script.Validate();

        Assert.False(result.IsValid);
        Assert.NotNull(result.SyntaxError);
    }

    [Fact]
    public void Validate_ShouldCaptureRuntimeError()
    {
        //TODO: 0 Zrobic error handling tak samo jak print
        string lua = @"
            function evaluate_data(api)
                error('Forced runtime error')
            end
        ";
        var path = WriteTempScript(lua);

        try
        {
            LuaLeaderboardScript.Load(path);
        }
        catch (LuaScriptValidationException ex)
        {
            Assert.False(ex.IsValid);
            Assert.True(ex.HasErrors);
        }

        Assert.True(false);
    }
}