package com.leeworld.server.controller

import org.springframework.web.bind.annotation.GetMapping
import org.springframework.web.bind.annotation.RestController
import org.springframework.web.bind.annotation.RequestMapping
import org.springframework.web.bind.annotation.PathVariable
import com.leeworld.server.service.BlockService

@RestController
@RequestMapping("/digg")
class BlockController {

    private val blockService: BlockService

    constructor(blockService: BlockService) {
        this.blockService = blockService
    }

    @GetMapping("/block/{x}/{y}/{z}/{detail}")
    fun GetBlock(
        @PathVariable x: Int,
        @PathVariable y: Int,
        @PathVariable z: Int,
        @PathVariable detail: Int
    ): String {
        val block = blockService.GetBlock(x, y, z, detail).map { "${it.toString(2).padStart(16, '0')}" }
        return block.reduce { result: String?, item: String -> if (result == null) "${item}" else "${result}${item}" }
    }

    @GetMapping("/block/{x}/{y}/{z}/")
    fun GetBlock(
        @PathVariable x: Int,
        @PathVariable y: Int,
        @PathVariable z: Int,
    ): String {
        val block = blockService.GetBlock(x, y, z).map { "${it.toString(2).padStart(16, '0')}" }
        return block.reduce { result: String?, item: String -> if (result == null) "${item}" else "${result}${item}" }
    }
}
