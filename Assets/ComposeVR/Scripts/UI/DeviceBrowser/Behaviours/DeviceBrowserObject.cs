﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using ComposeVR;

namespace ComposeVR {

    public sealed class DeviceBrowserObject : MonoBehaviour, IDeviceBrowser {

        public DeviceBrowserController Controller;

        private BrowserColumnObject resultsColumn;
        private List<BrowserColumnObject> filterColumns;

        void Awake() {
            Controller.SetDeviceBrowser(this);
            Controller.Initialize();
        }

        BrowserColumnController IDeviceBrowser.GetResultColumn() {
            foreach(BrowserColumnObject c in GetComponentsInChildren<BrowserColumnObject>()) {
                if (c.Controller.Config.Type == BrowserColumnController.ColumnType.RESULTS) {
                    return c.Controller;
                }
            }

            return null;
        }

        List<BrowserColumnController> IDeviceBrowser.GetFilterColumns() {
            var filterColumns = new List<BrowserColumnController>();

            foreach(BrowserColumnObject c in GetComponentsInChildren<BrowserColumnObject>()) {
                if(c.Controller.Config.Type == BrowserColumnController.ColumnType.FILTER) {
                    filterColumns.Add(c.Controller);
                }
            }

            return filterColumns;
        }
    }

    public interface IDeviceBrowser {
        BrowserColumnController GetResultColumn();
        List<BrowserColumnController> GetFilterColumns();
    }
}