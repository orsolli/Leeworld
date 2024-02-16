package com.leeworld.server.service

import java.util.function.Consumer
import java.util.Stack
import org.springframework.stereotype.Service
import com.leeworld.server.repository.BlockRepository
import com.leeworld.server.service.isLeaf
import kotlin.collections.remove
import kotlin.collections.removeAll

@Service
class BlockService(private val blockRepository: BlockRepository) {
    fun GetBlock(x: Int, y: Int, z: Int, detail: Int = 0): ArrayList<Short> {
        val octreeStream = blockRepository.getBlock(x, y, z)
        var nNodesInThisLevel = 1
        var nNodesInNextLevel = 0
        var level = 0
        val result = ArrayList<Short>()
        val stream = octreeStream.peek({ node: Short ->
            result.add(node)
            for (power in 1..<16 step 2) {
                if (1.shl(power).and(node.toInt()) > 0) {
                    nNodesInNextLevel++
                }
            }
            nNodesInThisLevel--;
            if (nNodesInThisLevel == 0) {
                nNodesInThisLevel = nNodesInNextLevel
                nNodesInNextLevel = 0
                level++
            }
        }).takeWhile { level != detail || level == 0 }
        stream.toList()
        stream.close()
        return result
    }

    fun MutateBlock(x: Int, y: Int, z: Int, path: List<Int>, value: Boolean) {
        val octreeStream = blockRepository.getBlock(x, y, z)
        blockRepository.setBlock(x, y, z, Mutate(octreeStream.toList().toMutableList(), path, value))
    }
}

fun Mutate(octree: List<Short>, path: List<Int>, value: Boolean): MutableList<Short> {
    val newOctree = octree.toMutableList()
    // Analyze
    var readCount = 0
    val levelIndexes = mutableListOf<Int>(0, 1)
    val pathIndexes = mutableListOf<Int>(0, 1 + numberOfNodesBefore(newOctree[0], path[0]))

    val root = newOctree[pathIndexes[0]]
    if (isLeaf(root, path[0])) {
        newOctree.set(0, 2.shl((7-path[0])*2).or(root.toInt()).toShort())
        newOctree.add(pathIndexes[1], if (isSet(root, path[0])) 0b0101_0101_0101_0101.toShort() else 0b0000_0000_0000_0000.toShort())
    }
    for (level in 1..<path.size) {
        val indexOfNextLevel = levelIndexes[level]
        pathIndexes.add(levelIndexes[level] + relativeIndexOfChild(newOctree, levelIndexes[level-1], pathIndexes[level], path[level]))

        val oldValue = newOctree[pathIndexes[level]]
        if (isLeaf(oldValue, path[level])) {
            newOctree.set(pathIndexes[level], 2.shl((7-path[level])*2).or(oldValue.toInt()).toShort())
            newOctree.add(pathIndexes[level+1], if (isSet(oldValue, path[level])) 0b0101_0101_0101_0101.toShort() else 0b0000_0000_0000_0000.toShort())
        }

        var n = 0
        do {
            val nChildren = numberOfNodesBefore(newOctree[readCount++])
            n += nChildren
        } while (indexOfNextLevel > readCount)

        levelIndexes.add(indexOfNextLevel + n)
    }

    // Mutate
    val currentIndex = pathIndexes[path.size-1]
    var currentValue = newOctree.get(currentIndex)
    if (isSet(currentValue, path.last()) == value && isLeaf(currentValue, path.last())) return octree.toMutableList()
    var flipper = 1.shl((7-path.last())*2)
    var newValue = if (value) flipper.or(currentValue.toInt()) else flipper.inv().and(currentValue.toInt())

    // Deep clean
    val discardPile = mutableListOf<Int>()
    if (!isLeaf(currentValue, path.last())) {
        flipper = 2.shl((7-path.last())*2)
        newValue = flipper.inv().and(newValue)
        val stack = Stack<Int>()
        stack.push(currentIndex)
        while (stack.isNotEmpty()) {
            val discardIndex = stack.pop()
            if (discardIndex != currentIndex)
                discardPile.add(discardIndex)
            if (readCount < newOctree.size) {
                val indexOfThisLevel = levelIndexes.last()
                var n = 0
                do {
                    val nChildren = numberOfNodesBefore(newOctree[readCount])
                    if (discardIndex == readCount) {
                        val childStart = indexOfThisLevel + n
                        for (i in nChildren-1 downTo 0)
                            stack.push(childStart + i)
                    }
                    n += nChildren
                    readCount += 1
                } while (indexOfThisLevel > readCount && readCount < newOctree.size)
                levelIndexes.add(indexOfThisLevel + n)
            }
        }
    }

    newOctree.set(currentIndex, newValue.toShort())

    // Backpropagate
    for (p in path.size-2 downTo 1) {
        val ancestorIndex = pathIndexes[p]
        val ancestorValue = newOctree.get(ancestorIndex)
        val ancestorIsSet = isSet(ancestorValue, path[p])

        val childIndex = pathIndexes[p+1]
        val childValue = newOctree.get(childIndex)
        val childIsSet = isSet(childValue, path[p+1])

        val nSetInAncestor = numberOfSet(ancestorValue)

        flipper = 1.shl((7-path[p])*2)
        var newAncestor = if (nSetInAncestor >= 4) flipper.or(ancestorValue.toInt()).toShort() else flipper.inv().and(ancestorValue.toInt()).toShort()

        // Remove detail
        if (childValue == 0b0000_0000_0000_0000.toShort() || childValue == 0b0101_0101_0101_0101.toShort()) {
            newOctree.removeAt(childIndex)
            for (d in 0..<discardPile.size) {
                if (discardPile[d] > childIndex) discardPile[d] -= 1
            }
            flipper =  2.shl((7-path[p])*2).inv()
            newAncestor = flipper.and(ancestorValue.toInt()).toShort()
        }

        newOctree.set(ancestorIndex, newAncestor)
        if (ancestorIsSet != childIsSet && ((nSetInAncestor == 4 && !childIsSet) || (nSetInAncestor == 3 && childIsSet))) continue
        break
    }
    for (d in discardPile.size-1 downTo 0) newOctree.removeAt(discardPile[d])
    return newOctree.toMutableList()
}

fun numberOfNodesBefore(node: Short, index: Int = 8): Int {
    var mask = 2 shl (8-index)*2 // 0000_0000_0000_0000_0000_0000_0000_0010
    mask = mask - 1 // 0000_0000_0000_0000_0000_0000_0000_0001
    mask = mask.inv() // 1111_1111_1111_1111_1111_1111_1111_1110
    mask = mask.and(0b1010_1010_1010_1010) // 0000_0000_0000_0000_1010_1010_1010_1010
    var result = mask.and(node.toInt()).toString(2).filter { it == '1' }
    return result.count()
}
fun relativeIndexOfChild(octree: List<Short>, indexOfLevel: Int, indexOfNode: Int, index: Int): Int {
    var n = 0
    for (i in indexOfLevel..<indexOfNode) {
        n += numberOfNodesBefore(octree[i])
    }
    return n + numberOfNodesBefore(octree[indexOfNode], index)
}
fun isLeaf(node: Short, index: Int): Boolean {
    var mask = 2 shl (7-index)*2 // 0000_0000_0000_0000_0000_0000_0000_0010
    var result = mask.and(node.toInt())
    return result == 0
}
fun isSet(node: Short, index: Int): Boolean {
    var mask = 1 shl (7-index)*2 // 0000_0000_0000_0000_0000_0000_0000_0001
    var result = mask.and(node.toInt())
    return result != 0
}

fun numberOfSet(node: Short): Int {
    var mask = 0.inv() // 1111_1111_1111_1111_1111_1111_1111_1111
    mask = mask.and(0b0101_0101_0101_0101) // 0000_0000_0000_0000_0101_0101_0101_0101
    var result = mask.and(node.toInt()).toString(2).filter { it == '1' }
    return result.count()
}

