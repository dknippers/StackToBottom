using System.Text;
using UnityEngine;

namespace StackToBottom.Extensions;
public static class GameCardExtensions
{
    /// <summary>
    /// Indicates if this stack can have the other stack on top, i.e. if the root of <paramref name="other"/> can be placed on the leaf of <paramref name="card"/>.
    /// This is similar to the built-in <see cref="CardData.CanHaveCardOnTop(CardData, bool)"/> except we also consider status cards in the stack of <paramref name="card"/>,
    /// which the game implements in a separate method <see cref="CardData.CanHaveCardsWhileHasStatus()"/>.
    /// </summary>
    /// <param name="card">This card / stack</param>
    /// <param name="other">The other card / stack</param>
    /// <returns></returns>
    public static bool CanHaveOnTop(this GameCard card, GameCard other)
    {
        var leaf = card.GetLeafCard();
        var root = other.GetRootCard();

        if (!leaf.CardData.CanHaveCardOnTop(root.CardData))
        {
            return false;
        }

        // The CanHaveCardOnTop method does not check for status cards for some reason
        // so we perform that check here to ensure `card` allows `other` to be placed on top of it.
        if (card.GetCardWithStatusInStack() is GameCard statusCard &&
           !statusCard.CardData.CanHaveCardsWhileHasStatus())
        {
            // Certain cards do not allow any cards to be placed on them when any card in the stack
            // has a status effect running right now.
            return false;
        }

        return true;
    }

    /// <summary>
    /// Indicates if <paramref name="card"/> and <paramref name="other"/> belong to the same stack of cards.
    /// </summary>
    /// <param name="card">This card / stack</param>
    /// <param name="other">Other card / stack</param>
    /// <returns></returns>
    public static bool IsSameStack(this GameCard card, GameCard other)
    {
        if (ReferenceEquals(card, other))
        {
            return true;
        }

        var r1 = card.GetRootCard();
        var r2 = other.GetRootCard();

        if (ReferenceEquals(r1, r2))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Indicates if <paramref name="card"/> can be stacked to the bottom of <paramref name="other"/>,
    /// i.e. whether <paramref name="other"/> can be stacked on top of <paramref name="card"/>.
    /// </summary>
    /// <param name="card">Card</param>
    /// <param name="other">Other card</param>
    /// <returns></returns>
    public static bool CanStackToBottomOf(this GameCard card, GameCard other)
    {
        return
            !card.IsDestroyed() &&
            !other.IsDestroyed() &&
            !other.IsEquipped &&
            !other.IsWorking &&
            !other.InConflict &&
            other.CanBeDragged() &&
            card.CardData.WorkerAmount == 0 &&
            !card.IsSameStack(other) &&
            !other.IsWorkingOnBlueprint() &&
            card.CanHaveOnTop(other);
    }

    /// <summary>
    /// Stacks <paramref name="card"/> to the bottom of <paramref name="target"/>.
    /// Note this method just performs the bottom stack without checking whether it is possible.
    /// Caller should ensure this using <see cref="CanStackToBottomOf(GameCard, GameCard)"/>.
    /// </summary>
    /// <param name="card">Card to stack to bottom of another stack</param>
    /// <param name="target">Target stack for bottom stack operation</param>
    public static void StackToBottomOf(this GameCard card, GameCard target)
    {
        if (card.IsDestroyed() || target.IsDestroyed())
        {
            return;
        }

        var cardLeaf = card.GetLeafCard();
        var cardRoot = card.GetRootCard();
        var targetRoot = target.GetRootCard();

        // When we stack `card` to the bottom of `target` what actually happens is `target` will be stacked on top of `card`.
        // When stacking A on B the game will keep B's position static and move A to fit underneath B but in our case
        // that would mean the game will move target towards card's position.
        // We actually want target to maintain its position because the user is dragging card at the moment, not target.
        // To achieve this we set the position of card's root card to the target's root card's position.
        cardRoot.SetPosition(targetRoot.transform.position);

        targetRoot.SetParent(cardLeaf);

        AudioManager.me.PlaySound2D(AudioManager.me.DropOnStack, UnityEngine.Random.Range(0.8f, 1.2f), 0.3f);
    }

    /// <summary>
    /// Indicates if this <paramref name="card"/> has been destroyed.
    /// </summary>
    /// <param name="card">Card</param>
    /// <returns><c>true</c> if this <see cref="GameCard"/> has been destroyed, otherwise <c>false</c></returns>
    public static bool IsDestroyed(this GameCard? card) =>
        card is null || card.GetInstanceID() == 0;

    /// <summary>
    /// Instantly sets the position of <paramref name="card"/> to the given <paramref name="position"/>.
    /// </summary>
    /// <param name="card">Card</param>
    /// <param name="position">Position</param>
    public static void SetPosition(this GameCard card, Vector3 position)
    {
        // Both the underlying transform position AND the custom TargetPosition
        // properties need to be set to instantly set a card's position correctly.
        card.transform.position = card.TargetPosition = position;
    }

    /// <summary>
    /// Indicates if <paramref name="card"/> and <paramref name="other"/> are instances of the same prefab.
    /// </summary>
    /// <param name="card">Card</param>
    /// <param name="other">Other card</param>
    /// <returns></returns>
    public static bool IsSamePrefab(this GameCard card, GameCard other)
    {
        if (ReferenceEquals(card, other)) return true;

        return card.CardData.Id == other.CardData.Id;
    }

    public static bool IsRoot(this GameCard card) => card.Parent is null;
    public static bool IsLeaf(this GameCard card) => card.Child is null;

    public static string GetName(this GameCard? card)
    {
        return card?.CardData?.Name ?? "NULL";
    }

    public static string GetDebugName(this GameCard? card)
    {
        if (card is null) return "NULL";

        return new StringBuilder()
            .Append('[')
            .Append(Math.Abs(card.GetInstanceID()))
            .Append(']')
            .Append(' ')
            .Append(card.GetName())
            .ToString();
    }

    /// <summary>
    /// Indicates if <paramref name="card"/> is part of a stack that is currently working on a Blueprint.
    /// </summary>
    /// <param name="card">Card to check</param>
    /// <returns><c>true</c> if currently working on a blueprint, otherwise <c>false</c></returns>
    public static bool IsWorkingOnBlueprint(this GameCard? card)
    {
        if (card?.GetRootCard()?.TimerBlueprintId is not string blueprintId ||
            string.IsNullOrEmpty(blueprintId))
        {
            return false;
        }

        return WorldManager.instance?.GetBlueprintWithId(blueprintId) is not null;
    }
}
