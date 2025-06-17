using MoonSharp.Interpreter;
using NuGet.Versioning;
using TournamentTool.Models.Ranking;

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

        public void TestMethod()
        {
            Called = true;
        }
    }

    // ----------- Description Tests -----------

    [Fact]
    public void GetDescription_ShouldReturnExpectedText()
    {
        string lua = @"
            description = 'Leaderboard v1 script'
            version = '1.0.0'
            function EvaluatePlayer(api) end
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
            function EvaluatePlayer(api) end
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
            function EvaluatePlayer(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.Equal(string.Empty, script.Description);
    }

    // ----------- Version Tests -----------

    [Fact]
    public void GetVersion_ShouldReturnCorrectVersion()
    {
        string lua = @"
            version = '2.5.1'
            function EvaluatePlayer(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.Equal(new NuGetVersion("2.5.1"), script.Version);
    }

    [Fact]
    public void GetVersion_ShouldReturnNewerVersion_IfDefined()
    {
        string lua = @"
            version = '10.0.0'
            function EvaluatePlayer(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.True(script.Version! > new NuGetVersion("1.0.0"));
    }

    [Fact]
    public void GetVersion_ShouldReturnOlderVersion_IfDefined()
    {
        string lua = @"
            version = '0.1.0'
            function EvaluatePlayer(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.True(script.Version! < new NuGetVersion("1.0.0"));
    }

    [Fact]
    public void GetVersion_ShouldReturnNull_IfNotDefined()
    {
        string lua = @"
            description = 'some script'
            function EvaluatePlayer(api) end
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
            function EvaluatePlayer(api) end
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
            function EvaluatePlayer(api) end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        Assert.Null(script.Version);
    }

    // ----------- EvaluatePlayer Tests -----------

    [Fact]
    public void Run_ShouldCallEvaluatePlayer()
    {
        string lua = @"
            function evaluate_player(api)
                api:test_method()
            end
        ";
        var path = WriteTempScript(lua);
        UserData.RegisterType<TestContext>();
        var script = LuaLeaderboardScript.Load(path);

        var context = new TestContext();
        script.Run(context);

        Assert.True(context.Called);
    }

    [Fact]
    public void Run_ShouldNotThrow_IfEvaluatePlayerIsEmpty()
    {
        string lua = @"
            function evaluate_player(api)
            end
        ";
        var path = WriteTempScript(lua);
        var script = LuaLeaderboardScript.Load(path);

        var context = new TestContext();

        var ex = Record.Exception(() => script.Run(context));
        Assert.Null(ex);
    }

    // ----------- Invalid Script Tests -----------

    [Fact]
    public void Load_ShouldThrow_IfLuaIsInvalid()
    {
        string lua = "this is not valid Lua!";
        var path = WriteTempScript(lua);

        Assert.Throws<SyntaxErrorException>(() => LuaLeaderboardScript.Load(path));
    }
}