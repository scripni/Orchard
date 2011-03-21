﻿(function ($) {
    $(function () {
        // append the shape tracing window container at the end of the document
        $('<div id="shape-tracing-container"> ' +
                '<div id="shape-tracing-resize-handle" ></div>' +
                '<div id="shape-tracing-toolbar">' +
                    '<div id="shape-tracing-toolbar-switch"></div>' +
                '</div>' +
                '<div id="shape-tracing-window">' +
                    '<div id="shape-tracing-window-tree"></div>' +
                    '<div id="shape-tracing-window-content"></div>' +
                '</div>' +
            '</div>' +
            '<div id="shape-tracing-container-ghost"></div>' +
            '<div id="shape-tracing-overlay"></div>'
        ).appendTo('body');

        // preload main objects
        var shapeTracingContainer = $('#shape-tracing-container');
        var shapeTracingResizeHandle = $('#shape-tracing-resize-handle');
        var shapeTracingToolbar = $('#shape-tracing-toolbar');
        var shapeTracingToolbarSwitch = $('#shape-tracing-toolbar-switch');
        var shapeTracingWindow = $('#shape-tracing-window');
        var shapeTracingWindowTree = $('#shape-tracing-window-tree');
        var shapeTracingWindowContent = $('#shape-tracing-window-content');
        var shapeTracingGhost = $('#shape-tracing-container-ghost');
        var shapeTracingOverlay = $('#shape-tracing-overlay');

        // store the size of the container when it is closed (default in css)
        var initialContainerSize = shapeTracingContainer.height();
        var previousSize = 0;

        // ensure the ghost has always the same size as the container
        // and the container is always positionned correctly
        var syncResize = function () {
            var _window = $(window);
            var containerHeight = shapeTracingContainer.outerHeight();
            var toolbarHeight = shapeTracingToolbar.outerHeight();
            var resizeHandleHeight = shapeTracingResizeHandle.outerHeight();

            shapeTracingGhost.height(containerHeight);

            var windowHeight = _window.height();
            var scrollTop = _window.scrollTop();

            shapeTracingContainer.offset({ top: windowHeight - containerHeight + scrollTop, left: 0 });
            shapeTracingWindow.height(containerHeight - toolbarHeight - resizeHandleHeight);
            shapeTracingWindowTree.height(containerHeight - toolbarHeight - resizeHandleHeight);
            shapeTracingWindowContent.height(containerHeight - toolbarHeight - resizeHandleHeight);
            shapeTracingWindowContent.width(shapeTracingContainer.outerWidth - shapeTracingWindowTree.outerWidth);
            shapeTracingContainer.width('100%');
        };

        // ensure the size/position is correct whenver the container or the browser is resized
        shapeTracingContainer.resize(syncResize);
        $(window).resize(syncResize);
        $(window).resize();

        // removes the position flickering by hiding it first, then showing when ready
        shapeTracingContainer.show();

        // expand/collapse behavior
        // ensure the container has always a valid size when expanded
        shapeTracingToolbarSwitch.click(function () {
            var _this = $(this);
            _this.toggleClass('expanded');
            if (_this.hasClass('expanded')) {
                shapeTracingContainer.height(Math.max(previousSize, 100, shapeTracingContainer.height()));
            }
            else {
                // save previous height
                previousSize = shapeTracingContainer.height();
                shapeTracingContainer.height(initialContainerSize);
            }

            syncResize();
        });

        // add a resizable handle to the container
        $('#shape-tracing-resize-handle').addClass('ui-resizable-handle ui-resizable-n');
        shapeTracingContainer.resizable({ handles: { n: '#shape-tracing-resize-handle'} });

        var shapeNodes = {}; // represents the main index of shape nodes, indexed by id

        // projects the shape ids to each DOM element
        var startShapeTracingBeacons = $('.shape-tracing-wrapper[shape-id]');
        startShapeTracingBeacons.each(function () {
            var _this = $(this);

            var shapeNode = {
                id: _this.attr('shape-id'),
                type: _this.attr('shape-type'),
                hint: _this.attr('shape-hint'),
                parent: null,
                children: {}
            };

            // register the new shape node into the main shape nodes index
            shapeNodes[shapeNode.id] = shapeNode;

            // assign the shape-id attribute to all direct children, except wrappers themselves (it would erase their own shape-id)
            var found = false;
            _this
                .nextUntil('[end-of="' + shapeNode.id + '"]') // all elements between the script beacons
                .children(':not(.shape-tracing-wrapper)') // all children but not inner beacons
                .andSelf() // add the first level items
                .attr('shape-id', shapeNode.id) // add the shape-id attribute
                .each(function () {
                    // assign a shapeNode instance to the DOM element
                    this.shapeNode = shapeNode;
                    found = true;
                });

            // if the shape is empty, add a hint
            if (!found) {
                shapeNode.hint = 'empty';
            }
            this.shapeNode = shapeNode;
        });

        // construct the shape tree based on all current nodes
        // for each start beacon, search for the first parent beacon, and create nodes if they don't exist
        startShapeTracingBeacons.each(function () {
            var _this = $(this);
            var shapeNode = this.shapeNode;
            var parent = _this.parent('[shape-id!=' + shapeNode.id + ']').get(0);

            shapeNodes[shapeNode.id] = shapeNode;

            if (parent.shapeNode) {
                var parentShapeNode = parent.shapeNode;
                shapeNodes[parentShapeNode.id] = parentShapeNode;
                shapeNode.parent = parentShapeNode;
                parentShapeNode.children[shapeNode.id] = shapeNode;
            }
        });

        // removes all beacons as we don't need them anymore
        $('.shape-tracing-wrapper').remove();

        // add first level shapes tree nodes
        var shapes = $('<ul></ul>');
        for (var shapeId in shapeNodes) {
            if (!shapeNodes[shapeId].parent) {
                shapes.append(createTreeNode(shapeNodes[shapeId]));
            }
        }

        shapeTracingWindowTree.append(shapes);

        // add the expand/collapse logic to the shapes tree
        var glyph = $('<span class="expando-glyph-container closed"><span class="expando-glyph"></span>&#8203;</span>');
        shapeTracingWindowTree.find('div').parent(':has(li)').prepend(glyph);

        // collapse all sub uls
        shapeTracingWindowTree.find('ul ul').toggle(false);

        // expands a list of shapes in the tree
        var openExpando = function (expando) {
            if (expando.hasClass("closed") || expando.hasClass("closing")) {
                expando.siblings('ul').slideDown(100, function () { expando.removeClass("opening").removeClass("closed").addClass("open"); });
                expando.addClass("opening");
            }
        }

        // collapses a list of shapes in the tree
        var closeExpando = function (expando) {
            if (!expando.hasClass("closed") && !expando.hasClass("closing")) {
                expando.siblings('ul').slideUp(100, function () { expando.removeClass("closing").removeClass("open").addClass("closed"); });
                expando.addClass("closing");
            }
        }

        //create an overlay on shapes' descendants
        var overlayTarget = null;
        $('[shape-id]').add(shapeTracingOverlay).mousemove(
            function (event) {
                event.stopPropagation();

                if ($(this).get(0) == shapeTracingOverlay.get(0)) {
                    shapeTracingOverlay.hide();
                }

                var element = document.elementFromPoint(event.pageX - $(window).scrollLeft(), event.pageY - $(window).scrollTop());
                shapeTracingOverlay.show();


                while (element && !element.shapeNode)
                    element = element.parentNode;

                if (!element || (overlayTarget != null && overlayTarget.get(0) == element)) {
                    return;
                }

                element = $(element);
                shapeTracingOverlay.offset(element.offset());
                shapeTracingOverlay.width(element.outerWidth()); // include border and padding 
                shapeTracingOverlay.height(element.outerHeight()); // include border and padding 

                overlayTarget = element;
            }
        );

        // selects a specific shape in the tree, highlight its elements, and display the information
        var selectShape = function (shapeId) {
            $('.shape-tracing-selected').removeClass('shape-tracing-selected');
            $('li[tree-shape-id="' + shapeId + '"] > div').add('[shape-id="' + shapeId + '"]').addClass('shape-tracing-selected');
            shapeTracingOverlay.hide();

            // show the properties for the selected shape
            $('[shape-id-meta]:visible').toggle(false);
            var target = $('[shape-id-meta="' + shapeId + '"]"');
            target.toggle(true);

            // enable codemirror for the current tab
            enableCodeMirror(target);
        }

        // select shapes when clicked
        shapeTracingOverlay.click(function () {
            var shapeNode = overlayTarget.get(0).shapeNode;
            selectShape(shapeNode.id);

            var lastExpanded = null;
            // open the tree until the selected element
            $('li[tree-shape-id="' + shapeNode.id + '"]').parents('li').andSelf().find('> .expando-glyph-container').each(function () {
                openExpando($(this));
            })
            .last()
            .each(function () {
                this.scrollIntoView()
            });

            return false;
        });

        //create an overlay on shape tree nodes
        shapeTracingWindowTree.find('[tree-shape-id] div').hover(
            function () {
                var _this = $(this);
                $('.shape-tracing-overlay').removeClass('shape-tracing-overlay');
                _this.addClass('shape-tracing-overlay');
            },
            function () {
                $('.shape-tracing-overlay').removeClass('shape-tracing-overlay');
            }
        );

        // select shape tree elements when clicked
        $('[tree-shape-id] > div').click(function (event) {
            var shapeId = $(this).parent().get(0).shapeNode.id;
            selectShape(shapeId);

            var element = $('[shape-id="' + shapeId + '"]').get(0);
            // there might be no DOM element if the shape was empty, or is not displayed
            if (element) {
                element.scrollIntoView();
            }

            event.stopPropagation();
        });

        // move all shape tracing meta blocks to the content window
        $("[shape-id-meta]").detach().prependTo(shapeTracingWindowContent);

        // add the expand/collapse logic to the shape model
        // var glyph = $('<span class="expando-glyph-container closed"><span class="expando-glyph"></span>&#8203;</span>');
        shapeTracingWindowContent.find('h3').parent(':has(li)').prepend(glyph);

        // collapse all sub uls
        shapeTracingWindowContent.find('ul ul').toggle(false);

        // tabs events
        shapeTracingWindowContent.find('.shape-tracing-tabs > li').click(function () {
            var _this = $(this);
            var tabName = this.className.split(/\s/)[0];

            // toggle the selected class on the tab li
            $('.shape-tracing-tabs > li.selected').removeClass('selected');
            $('.shape-tracing-tabs > li.' + tabName).addClass('selected');

            // hide all tabs and display the selected one
            $('.shape-tracing-meta-content > div').toggle(false);
            $('.shape-tracing-meta-content > div.' + tabName).toggle(true);

            // look for the targetted panel
            var wrapper = _this.parent().parent().first();
            var panel = wrapper.find('div.' + tabName);

            // enable codemirror for the current tab
            enableCodeMirror(panel);
        });

        // activates codemirror on specific textareas
        var enableCodeMirror = function (target) {
            // if there is a script, and colorization is not enabled yet, turn it on
            // code mirror seems to work only if the textarea is visible
            target.find('textarea:visible').each(function () {
                if ($(this).next('.CodeMirror').length == 0) {
                    CodeMirror.fromTextArea(this, { mode: "razor", tabMode: "indent", height: "100%", readOnly: true });
                }
            });
        }

        // automatically expand or collapse shapes in the tree
        shapeTracingWindow.find('.expando-glyph-container').click(function () {
            var _this = $(this);
            if (_this.hasClass("closed") || _this.hasClass("closing")) {
                openExpando(_this);
            }
            else {
                closeExpando(_this);
            }

            return false;
        });
    });

    // recursively create a node for the shapes tree
    function createTreeNode(shapeNode) {
        var node = $('<li></li>');
        node.attr('tree-shape-id', shapeNode.id);
        node.get(0).shapeNode = shapeNode;

        var text = shapeNode.type;
        // add the hint to the tree node if available
        if (shapeNode.hint != '') {
            text += ' [' + shapeNode.hint + ']';
        }

        node.append('<div>' + text + '</div>');
        var list = $('<ul></ul>');
        node.append(list);
        if (shapeNode.children) {
            for (var shapeId in shapeNode.children) {
                var child = createTreeNode(shapeNode.children[shapeId]);
                list.append(child);
            }
        }
        return node;
    }

})(jQuery);
