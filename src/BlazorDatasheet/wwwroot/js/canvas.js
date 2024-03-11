class SheetCanvas {

    /**
     *
     * @param {HTMLCanvasElement} canvasEl
     * @param {number} width
     * @param {number} height
     */
    constructor(canvasEl) {

        this.canvas = canvasEl
        this.ctx = canvasEl.getContext('2d')

        const rect = canvasEl.getBoundingClientRect()
        this.setCanvasSize(ca
        
        
        
        nvasEl, rect.width, rect.height)

        canvasEl.addEventListener('resize', e => {
            console.log('sss')
        })
    }

    handleResize() {
        console.log('canvas resized')
    }

    /**
     * @param {HTMLCanvasElement} canvasEl
     * @param {number} width
     * @param {number} height
     */
    setCanvasSize(width, height) {
        const dpr = this.getPixelRatio()
        this.canvas.width = width * dpr
        this.canvas.height = height * dpr

        this.ctx.scale(dpr, dpr)
        this.canvas.style.width = `${width}px`
        this.canvas.style.height = `${height}px`

        this.width = this.canvas.width
        this.height = this.canvas.height
    }

    getPixelRatio() {
        let dpr = window.devicePixelRatio || 1
        let bsr = this.ctx.webkitBackingStorePixelRatio ||
            this.ctx.mozBackingStorePixelRatio ||
            this.ctx.msBackingStorePixelRatio ||
            this.ctx.oBackingStorePixelRatio ||
            this.ctx.backingStorePixelRatio || 1;

        return dpr / bsr;
    }

    /**
     *
     * @param {number[]} rowPositions
     * @param {number[]} colPositions
     * @param {string[]} text
     */
    drawGrid(rowPositions, colPositions, text) {
        this.ctx.strokeStyle = "#ddd"
        this.ctx.lineWidth = 1
        for (let i = 1; i < rowPositions.length; i++) {
            let y = rowPositions[i] - rowPositions[0]
            this.ctx.beginPath()
            this.ctx.moveTo(0, y)
            this.ctx.lineTo(this.width, y)
            this.ctx.stroke()
            this.ctx.closePath()
        }

        for (let i = 1; i < colPositions.length; i++) {
            let x = colPositions[i] - colPositions[0]
            this.ctx.beginPath()
            this.ctx.moveTo(x, 0)
            this.ctx.lineTo(x, this.height)
            this.ctx.stroke()
            this.ctx.closePath()
        }

        this.ctx.strokeStyle = "#666"
        let i = 0
        this.ctx.font = "12px Arial"
        rowPositions.forEach(rowPos => {
            colPositions.forEach(colPos => {
                this.ctx.fillText(text[i], colPos - colPositions[0], rowPos - rowPositions[0])
                i++
            })
        })


    }


}

export function getSheetCanvas(canvasEl) {
    return new SheetCanvas(canvasEl)
}