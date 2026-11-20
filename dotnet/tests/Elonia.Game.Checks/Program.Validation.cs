using System;
using System.Collections.Generic;
using System.IO;
using Elonia.Game;
using Elonia.Simulation;

namespace Elonia.Checks;

internal static partial class Program
{
    private static void CheckRuleValidation(GameContent content)
    {
        CheckInvalidCell<ArgumentException>("Floor", "grass", "Name", "", "A floor type needs a name.");
        CheckInvalidCell<ArgumentException>("Prop", "tree", "Name", "", "A prop type needs a name.");
        CheckInvalidCell<ArgumentException>("Actor", "hero", "Name", "", "An actor type needs a name.");
        CheckInvalidCell<ArgumentException>("Thing", "potion", "Name", "", "A thing type needs a name.");
        CheckInvalidCell<ArgumentOutOfRangeException>("Actor", "hero", "MaxHp", "0", "A max HP must be positive.");
        CheckInvalidCell<ArgumentOutOfRangeException>("Actor", "hero", "Attack", "-1", "An attack power must not be negative.");
        CheckInvalidCell<ArgumentOutOfRangeException>("Actor", "hero", "Speed", "0", "An actor speed must be positive.");
        CheckInvalidCell<InvalidDataException>("Actor", "slime", "SenseRange", "0", "SenseRange 값 0");
        CheckInvalidCell<ArgumentOutOfRangeException>("Item", "potion", "Amount", "0", "An item amount must be positive.");
        CheckInvalidCell<InvalidDataException>("Thing", "spike_trap", "StepDamage", "-1", "StepDamage는 0 이상");
        CheckInvalidCell<InvalidDataException>("Prop", "tree", "DropThing", "", "DropThing이 필요합니다.");
        CheckInvalidCell<ArgumentOutOfRangeException>("Prop", "tree", "DropAmount", "0", "A harvest must drop a positive count.");
        CheckInvalidCell<ArgumentException>("Prop", "tree", "IsWalkable", "true", "A harvestable prop must not be walkable.");
        CheckInvalidCell<InvalidDataException>("TownLegend", "grass", "Symbol", "..", "한 글자 기호가 필요합니다.");
        CheckInvalidCell<InvalidDataException>("TownLegend", "wood_house", "Prop", "", "집 외형을 사용할 물체가 필요합니다.");

        Actor player = CreatePlay(content, content.Tables, "hero").Session.Player;
        CheckRejected<ArgumentException>(() => new MoveAction(1, 1), "대각선 이동");
        CheckRejected<ArgumentException>(() => new MoveAction(0, 0), "제자리 이동");
        CheckRejected<ArgumentException>(() => new MoveAction(2, 0), "두 칸 이동");
        CheckRejected<ArgumentNullException>(() => new AttackAction(null!), "공격 대상 누락");
        CheckRejected<ArgumentNullException>(() => new AttackBehavior(null!), "공격 AI 대상 누락");
        CheckRejected<ArgumentNullException>(() => new ChaseBehavior(null!, 2), "추격 대상 누락");
        CheckRejected<ArgumentOutOfRangeException>(() => new ChaseBehavior(player, 0), "추격 범위 0");
        CheckRejected<ArgumentNullException>(() => new FleeBehavior(null!, 2), "도주 대상 누락");
        CheckRejected<ArgumentOutOfRangeException>(() => new FleeBehavior(player, 0), "도주 범위 0");
        CheckRejected<ArgumentNullException>(() => new WanderBehavior(null!), "배회 난수 누락");
        CheckRejected<ArgumentOutOfRangeException>(() => DamageEffect.Apply(default, player, 0), "피해량 0");
    }

    private static void CheckInvalidCell<TException>(string sheet, string id, string field, string value, string message)
        where TException : Exception
    {
        Dictionary<(string, string, string), string> cells = new Dictionary<(string, string, string), string>
        {
            [(sheet, id, field)] = value
        };
        TException exception = CheckRejected<TException>(() => WorkbookFixture.CreateContent(cells), $"{sheet}.{id}.{field}");
        Check(exception.Message.Contains(message, StringComparison.Ordinal), $"표 오류에 기존 진단 문구가 있어야 한다. {exception.Message}");
    }

    private static TException CheckRejected<TException>(Action action, string scenario) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            _checkCount++;
            return exception;
        }

        throw new InvalidOperationException($"{scenario}을 허용했다. 예상한 예외는 {typeof(TException).Name}이다.");
    }
}
